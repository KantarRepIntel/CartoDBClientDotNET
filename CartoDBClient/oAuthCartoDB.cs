using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Net;
using System.IO;
using System.Collections.Specialized;
using System.Xml;



namespace CartoDBClient
{
    public class oAuthCartoDB : OAuthBase
    {
        public enum Method { GET, POST };
        public string REQUEST_TOKEN = "";
        public string AUTHORIZE = "";
        public string ACCESS_TOKEN = "https://" + Properties.Settings.Default.domain + ".cartodb.com/oauth/access_token";
        public string XAUTH_ACCESS_TOKEN = "https://" + Properties.Settings.Default.domain + ".cartodb.com/oauth/access_token";
        public string SCOPE = "https://" + Properties.Settings.Default.domain + ".cartodb.com/api/v1/sql";

        private string _consumerKey = "";
        private string _consumerSecret = "";
        private string _token = "";
        private string _tokenSecret = "";
        private string _verifier = "";
        private string _xAuthUsername = "";
        private string _xAuthPassword = "";

#region Properties
        public string ConsumerKey 
        {
            get
            {
                if (_consumerKey.Length == 0)
                {
                    _consumerKey = Properties.Settings.Default.consumerKey; 
                }
                return _consumerKey; 
            } 
            set { _consumerKey = value; } 
        }
        
        public string ConsumerSecret { 
            get {
                if (_consumerSecret.Length == 0)
                {
                    _consumerSecret = Properties.Settings.Default.consumerSecret;
                }
                return _consumerSecret; 
            } 
            set { _consumerSecret = value; } 
        }

        public string Token { get { return _token; } set { _token = value; } }
        public string TokenSecret { get { return _tokenSecret; } set { _tokenSecret = value; } }
        public string Verifier { get { return _verifier; } set { _verifier = value; } }
        public string xAuthUsername { get { return _xAuthUsername; } set { _xAuthUsername = value; } }
        public string xAuthPassword { get { return _xAuthPassword; } set { _xAuthPassword = value; } }

#endregion

        /// <summary>
        /// Get the link to CartoDB's authorization page for this application.
        /// </summary>
        /// <returns>The url with a valid request token, or a null string.</returns>
        public string AuthorizationLinkGet()
        {
            string ret = null;

            string response = oAuthWebRequest(Method.GET, REQUEST_TOKEN, String.Empty);
            if (response.Length > 0)
            {
                //response contains token and token secret.  We only need the token.
                NameValueCollection qs = HttpUtility.ParseQueryString(response);
                if (qs["oauth_token"] != null)
                {
                    ret = AUTHORIZE + "?oauth_token=" + qs["oauth_token"];
                }
            }
            return ret;
        }
       

        /// <summary>
        /// Exchange the request token for an access token.
        /// </summary>
        /// <param name="authToken">The oauth_token is supplied by CartoDB's authorization page following the callback.</param>
        public void AccessTokenGet(string authToken, string verifier)
        {
            this.Token = authToken;
            this.Verifier = verifier;

            string response = oAuthWebRequest(Method.GET, ACCESS_TOKEN, String.Empty);

            if (response.Length > 0)
            {
                //Store the Token and Token Secret
                NameValueCollection qs = HttpUtility.ParseQueryString(response);
                if (qs["oauth_token"] != null)
                {
                    this.Token = qs["oauth_token"];
                }
                if (qs["oauth_token_secret"] != null)
                {
                    this.TokenSecret = qs["oauth_token_secret"];
                }
            }
        }


        /// <summary>
        /// Exchange the username and password for an access token.
        /// </summary>
        public void xAuthAccessTokenGet()
        {
            this.xAuthUsername = Properties.Settings.Default.mail;
            this.xAuthPassword = Properties.Settings.Default.pass;  

            string response = oAuthWebRequest(Method.GET, XAUTH_ACCESS_TOKEN, String.Empty);

            if (response.Length > 0)
            {
                //Store the Token and Token Secret
                NameValueCollection qs = HttpUtility.ParseQueryString(response);
                if (qs["oauth_token"] != null)
                {
                    this.Token = qs["oauth_token"];
                }
                if (qs["oauth_token_secret"] != null)
                {
                    this.TokenSecret = qs["oauth_token_secret"];
                }
            }
        }


        /// <summary>
        /// Exchange the username and password for an access token.
        /// </summary>
        public string oAuthCartoDBQuery(string query)
        {
            this.xAuthUsername = "";
            this.xAuthPassword = "";



           string url = SCOPE + "?q=" + query;
           string jsonString = this.oAuthWebRequest(oAuthCartoDB.Method.GET, url, String.Empty);

           return jsonString;
        }






        /// <summary>
        /// Submit a web request using oAuth.
        /// </summary>
        /// <param name="method">GET or POST</param>
        /// <param name="url">The full url, including the querystring.</param>
        /// <param name="postData">Data to post (querystring format)</param>
        /// <returns>The web server response.</returns>
        public string oAuthWebRequest(Method method, string url, string postData)
        {
            string outUrl = "";
            string querystring = "";
            string ret = "";


            //Setup postData for signing.
            //Add the postData to the querystring.
            if (method == Method.POST)
            {
                if (postData.Length > 0)
                {
                    //Decode the parameters and re-encode using the oAuth UrlEncode method.
                    NameValueCollection qs = HttpUtility.ParseQueryString(postData);
                    postData = "";
                    foreach (string key in qs.AllKeys)
                    {
                        if (postData.Length > 0)
                        {
                            postData += "&";
                        }
                        qs[key] = HttpUtility.UrlDecode(qs[key]);
                        qs[key] = this.UrlEncode(qs[key]);
                        postData += key + "=" + qs[key];

                    }
                    if (url.IndexOf("?") > 0)
                    {
                        url += "&";
                    }
                    else
                    {
                        url += "?";
                    }
                    url += postData;
                }
            }

            Uri uri = new Uri(url);
            
            string nonce = this.GenerateNonce();
            string timeStamp = this.GenerateTimeStamp();

            //Generate Signature
            string sig = this.GenerateSignature(uri,
                this.ConsumerKey,
                this.ConsumerSecret,
                this.Token,
                this.TokenSecret,
                this.Verifier,
                this.xAuthUsername,
                this.xAuthPassword,
                method.ToString(),
                timeStamp,
                nonce,
                out outUrl,
                out querystring);

            querystring += "&oauth_signature=" + HttpUtility.UrlEncode(sig);

            //Convert the querystring to postData
            if (method == Method.POST)
            {
                postData = querystring;
                querystring = "";
            }

            if (querystring.Length > 0)
            {
                outUrl += "?";
            }

            ret = WebRequest(method, outUrl +  querystring, postData);

            return ret;
        }





  

        /// <summary>
        /// Web Request Wrapper
        /// </summary>
        /// <param name="method">Http Method</param>
        /// <param name="url">Full url to the web resource</param>
        /// <param name="postData">Data to post in querystring format</param>
        /// <returns>The web server response.</returns>
        public string WebRequest(Method method, string url, string postData)
        {
            HttpWebRequest webRequest = null;
            StreamWriter requestWriter = null;
            string responseData = "";

            webRequest = System.Net.WebRequest.Create(url) as HttpWebRequest;
            webRequest.Method = method.ToString();
            webRequest.ServicePoint.Expect100Continue = false;
            //webRequest.UserAgent  = "Identify your application please.";
            //webRequest.Timeout = 20000;

            if (method == Method.POST)
            {
                webRequest.ContentType = "application/x-www-form-urlencoded";

                //POST the data.
                requestWriter = new StreamWriter(webRequest.GetRequestStream());
                try
                {
                    requestWriter.Write(postData);
                }
                catch
                {
                    throw;
                }
                finally
                {
                    requestWriter.Close();
                    requestWriter = null;
                }
            }

            responseData = WebResponseGet(webRequest);

            webRequest = null;

            return responseData;

        }

        /// <summary>
        /// Process the web response.
        /// </summary>
        /// <param name="webRequest">The request object.</param>
        /// <returns>The response data.</returns>
        public string WebResponseGet(HttpWebRequest webRequest)
        {
            StreamReader responseReader = null;
            string responseData = "";

            try
            {
                responseReader = new StreamReader(webRequest.GetResponse().GetResponseStream());
                responseData = responseReader.ReadToEnd();
            }
            catch
            {
                throw;
            }
            finally
            {
                webRequest.GetResponse().GetResponseStream().Close();
                responseReader.Close();
                responseReader = null;
            }

            return responseData;
        }
    }
}
