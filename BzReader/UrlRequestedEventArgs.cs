using System;
using System.Collections.Generic;
using System.Text;

namespace BzReader
{
    /// <summary>
    /// The delegate for the UrlRequested event
    /// </summary>
    /// <param name="sender">The object which received the web browser request</param>
    /// <param name="e">Url which was requested</param>
    public delegate void UrlRequestedHandler(object sender, UrlRequestedEventArgs e);

    /// <summary>
    /// The class which used to pass information about the requested url to web browser
    /// </summary>
    public class UrlRequestedEventArgs : EventArgs
    {
        /// <summary>
        /// The url which was request by web browser
        /// </summary>
        private string url;
        /// <summary>
        /// Whether this is a redirect response
        /// </summary>
        private bool redirect;
        /// <summary>
        /// The redirect target
        /// </summary>
        private string redirectTarget;
        /// <summary>
        /// The response string
        /// </summary>
        private string response;

        /// <summary>
        /// The requested url
        /// </summary>
        public string Url
        {
            get { return url; }
        }

        /// <summary>
        /// Whether this is a redirect response
        /// </summary>
        public bool Redirect
        {
            get { return redirect; }
            set { redirect = value; }
        }

        /// <summary>
        /// Redirection target
        /// </summary>
        public string RedirectTarget
        {
            get { return redirectTarget; }
            set { redirectTarget = value; }
        }

        /// <summary>
        /// Response string
        /// </summary>
        public string Response
        {
            get { return response; }
            set { response = value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UrlRequestedEventArgs"/> class.
        /// </summary>
        /// <param name="aUrl">A URL which was request by web browser.</param>
        public UrlRequestedEventArgs(string aUrl)
            : base()
        {
            url = aUrl;
        }
    };
}
