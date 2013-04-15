using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace BzReader
{
    public partial class BrowseForm : Form
    {
        /// <summary>
        /// The number of milliseconds to wait before an automatic search is initiated on the query string
        /// </summary>
        private const int AUTOSEARCH_DELAY = 400;
        /// <summary>
        /// The list of the currently opened indexes
        /// </summary>
        Dictionary<string, Indexer> indexes = new Dictionary<string, Indexer>();
        /// <summary>
        /// Delayed automatic searching support
        /// </summary>
        private DateTime? lastTextChange;
        /// <summary>
        /// Whether the search using the current text has already been executed
        /// </summary>
        private bool searchLaunched;
        /// <summary>
        /// Set to true when HitsList.SelectedValueChanged is due to us loading the new result set
        /// </summary>
        private bool loadingResults;

        public BrowseForm()
        {
            InitializeComponent();

            if (Properties.Settings.Default.Dumps != null)
            {
                foreach (string file in Properties.Settings.Default.Dumps)
                {
                    LoadIndexer(file);
                }
            }

            WebServer.Instance.UrlRequested += new UrlRequestedHandler(WebServer_UrlRequested);
            webBrowser.DocumentTitleChanged += new EventHandler(webBrowser_DocumentTitleChanged);

            SyncCloseMenuItem();
        }

        /// <summary>
        /// Executes the search using the currently entered text
        /// </summary>
        private void LaunchSearch()
        {
            if (indexes.Count == 0)
            {
                return;
            }

            searchStatusLabel.Text = "Searching for '" + searchBox.Text + "'";

            searchLaunched = true;
            loadingResults = true;

            HitCollection hits = Indexer.Search(searchBox.Text, indexes.Values, Indexer.MAX_SEARCH_HITS);

            hitsBox.DataSource = hits;
            hitsBox.SelectedItem = null;

            loadingResults = false;

            searchStatusLabel.Text = hits.HadMoreHits ? "Showing " + Indexer.MAX_SEARCH_HITS.ToString() + " top results" : String.Empty;
        }

        /// <summary>
        /// Cancels any pending search request
        /// </summary>
        private void CancelSearch()
        {
            searchStatusLabel.Text = String.Empty;

            searchLaunched = false;
        }

        /// <summary>
        /// Gets called whenever the browser control requests a URL from the web server
        /// </summary>
        /// <param name="sender">Web server instance</param>
        /// <param name="e">Request parameters</param>
        private void Web_UrlRequested(object sender, UrlRequestedEventArgs e)
        {
            string response = "Not found";
            string redirect = String.Empty;

            PageInfo page = hitsBox.SelectedItem as PageInfo;

            if (page == null ||
                !e.Url.Equals(page.Name, StringComparison.InvariantCultureIgnoreCase))
            {
                HitCollection hits = Indexer.Search(e.Url, indexes.Values, 1);

                page = null;

                if (hits.Count > 0)
                {
                    page = hits[0];
                }
            }

            if (page != null)
            {
                response = page.GetFormattedContent();
                redirect = page.RedirectToTopic;
            }

            e.Redirect = !String.IsNullOrEmpty(redirect);
            e.RedirectTarget = redirect;
            e.Response = response;
        }

        /// <summary>
        /// Calls the Web browser control to specify new location to load
        /// </summary>
        /// <param name="sender">The HitsBox list</param>
        /// <param name="e">Event arguments</param>
        private void hitsBox_SelectedValueChanged(object sender, EventArgs e)
        {
            PageInfo page = hitsBox.SelectedItem as PageInfo;

            if (page != null &&
                !loadingResults)
            {
                webBrowser.Navigate(WebServer.Instance.GenerateUrl(page.Name));
            }
        }

        #region Helper methods

        private void searchBox_TextChanged(object sender, EventArgs e)
        {
            if (searchLaunched)
            {
                CancelSearch();
            }

            searchLaunched = false;

            lastTextChange = DateTime.Now;
        }

        private void searchBox_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Return ||
                e.KeyCode == Keys.Enter)
            {
                LaunchSearch();
            }
        }

        private void webBrowser_DocumentTitleChanged(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(webBrowser.DocumentTitle))
            {
                Text = "BzReader - " + webBrowser.DocumentTitle;
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new AboutBox().ShowDialog(this);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void BrowseForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            WebServer.Instance.Stop();

            StringCollection sc = new StringCollection();

            foreach (string file in indexes.Keys)
            {
                sc.Add(file.ToLowerInvariant());
            }

            Properties.Settings.Default.Dumps = sc;

            Properties.Settings.Default.Save();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog fd = new OpenFileDialog();

            fd.CheckFileExists = true;
            fd.CheckPathExists = true;
            fd.DereferenceLinks = true;
            fd.Multiselect = true;
            fd.SupportMultiDottedExtensions = true;
            fd.ValidateNames = true;
            fd.Filter = "Wikipedia dump files (*.xml.bz2)|*.xml.bz2|All files (*.*)|*.*";

            if (fd.ShowDialog(this) == DialogResult.OK)
            {
                foreach (string file in fd.FileNames)
                {
                    if (indexes.ContainsKey(file.ToLowerInvariant()))
                    {
                        continue;
                    }

                    LoadIndexer(file);
                }

                SyncCloseMenuItem();
            }
        }

        private void LoadIndexer(string file)
        {
            if (!File.Exists(file))
            {
                MessageBox.Show(this, "Dump file " + file + " does not exist", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                return;
            }

            Indexer ixr = new Indexer(file);

            if (!ixr.IndexExists)
            {
                if (new ProgressDialog(ixr).ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
            }

            indexes.Add(file.ToLowerInvariant(), ixr);
        }

        private void SyncCloseMenuItem()
        {
            foreach (ToolStripItem tsi in closeToolStripMenuItem.DropDownItems)
            {
                tsi.Click -= new EventHandler(dumpFileClose_Click);
            }

            closeToolStripMenuItem.DropDownItems.Clear();

            SortedDictionary<string, string> sd = new SortedDictionary<string,string>();

            foreach (string file in indexes.Keys)
            {
                sd.Add(Path.GetFileNameWithoutExtension(file), file);
            }

            foreach (string file in sd.Keys)
            {
                ToolStripItem tsi = new ToolStripMenuItem(file);

                tsi.Name = sd[file];

                closeToolStripMenuItem.DropDownItems.Add(tsi);
            }

            foreach (ToolStripItem tsi in closeToolStripMenuItem.DropDownItems)
            {
                tsi.Click += new EventHandler(dumpFileClose_Click);
            }

            closeToolStripMenuItem.Enabled = (closeToolStripMenuItem.DropDownItems.Count > 0);
        }

        private void dumpFileClose_Click(object sender, EventArgs e)
        {
            ToolStripItem tsi = (ToolStripItem)sender;

            indexes[tsi.Name].Close();

            indexes.Remove(tsi.Name);

            SyncCloseMenuItem();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (searchLaunched)
            {
                return;
            }

            if (lastTextChange.HasValue &&
                DateTime.Now.Subtract(lastTextChange.Value) > TimeSpan.FromMilliseconds(AUTOSEARCH_DELAY) &&
                searchBox.Text.Length > 2)
            {
                LaunchSearch();
            }
        }

        private void WebServer_UrlRequested(object sender, UrlRequestedEventArgs e)
        {
            Invoke(new UrlRequestedHandler(Web_UrlRequested), sender, e);
        }

        #endregion
    }
}
