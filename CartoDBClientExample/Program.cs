using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CartoDBClient;

namespace CartoDBClientExample
{
    class Program
    {
        static void Main(string[] args)
        {

            // Update property settings in Properties/Settings.settings at CartoDBClient Project
            // with access values gived by  CartoDB

            // mail: username to access CartoDB
            // pass: password to access CartoDB
            // consumerKey: consumer key in "Your api keys"
            // consumerSecret: consumer secret in "Your api keys"
            // domain: your subdomain in  cartodb.com: http://yoursubdomain.cartodb.com

            // Create the client using xAuth protocol.
            // Update property settings in Properties/Settings.settings at CartoDBClient Project

            oAuthCartoDB oAuth = new oAuthCartoDB();
            oAuth.xAuthAccessTokenGet();

            // Now we can perform queries straigh away. 
            // The result return the raw json string
            string jsonString = oAuth.oAuthCartoDBQuery("select * from table");


        }
    }
}
