using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BEERLabs.ProjectEsky.Networking.WebAPI
{
    public class MultiPartEntry
    {
        public readonly Headers headers;

        public string Value { get; set; }

        public string Name { get; set; }

        public string Filename { get; set; }

        public MultiPartEntry ()
        {
            this.headers = new Headers ();
        }
        public static Dictionary<string, MultiPartEntry> Parse (Request request)
        {
            var mps = new Dictionary<string, MultiPartEntry> ();
            var contentType = request.headers.Get ("Content-Type");
            if (contentType.Contains ("multipart/form-data")) {
                
                var boundary = request.body.Substring(0, request.body.IndexOf("\r\n")) + "\r\n";
                var parts = request.body.Split (new string[] { boundary }, System.StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts) {
                    var sep = part.IndexOf ("\r\n\r\n");
                    if (sep == -1)
                        continue;
                    var headerText = part.Substring (0, sep);
                    var mp = new MultiPartEntry ();
                    mp.headers.Read (headerText);
                    mp.Value = part.Substring (sep);
                    if (mp.headers.Contains ("Content-Disposition")) {
                        var s = mp.headers.Get ("Content-Disposition");
                        var nm = new Regex (@"(?<=name\=\"")(.*?)(?=\"")").Match (s);
                        if (nm.Success)
                            mp.Name = nm.Value.Trim ();
                        var fm = new Regex (@"(?<=filename\=\"")(.*?)(?=\"")").Match (s);
                        if (fm.Success)
                            mp.Filename = fm.Value.Trim ();
                    }
                    if (mp.Name != null)
                        mps.Add (mp.Name, mp);
                }
                
            }
            return mps;
        }
        public static Dictionary<string, MultiPartEntry> ParseFields (Request request)
        {
            var mps = new Dictionary<string, MultiPartEntry> ();
            var contentType = request.headers.Get ("Content-Type");
            string bod = request.body;
            string[] parts = bod.Split('&');
            foreach(string s in parts){
                string[] vals = s.Split('=');
                MultiPartEntry mpe = new MultiPartEntry();
                mpe.Name = vals[0];
                mpe.Value = vals[1];
                mps.Add(vals[0],mpe);
            }
            return mps;
        }
        public static Dictionary<string, MultiPartEntry> ParseJson(JSONRequest request){
            Dictionary<string, MultiPartEntry> returndict = new Dictionary<string, MultiPartEntry>();
            request.FillMultipartRequest(ref returndict);
            return returndict;
        }
    }
}