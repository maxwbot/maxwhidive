using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Maxwhidive
{
    public class SubtitleConverter
    {
        //State for WSRT & Webvvt reading
        private enum SState { Empty, Adding, Iterating, Comment, Timestamp };
        
        public enum SubtitleNewLineOption {Default, Windows, Unix, MacOLD}

        private static string[] SpaceArray = new string[] { " " }; //Dont want to keep recreating these
        private static string[] NewLineArray = new string[] { "\n" };
        private static string[] CommaArray = new string[] { "," };
        private static string[] CloseSquigArray = new string[] { "}" };

        public SubtitleNewLineOption subtitleNewLineOption = SubtitleNewLineOption.Default;

        public Encoding EncodingRead = Encoding.UTF8;
        //Internal sub format to allow easy conversion
        private class SubtitleEntry
        {
            public DateTime startTime { get; set; }
            public DateTime endTime { get; set; }
            public string content { get; set; }
            public SubtitleEntry(DateTime sTime, DateTime eTime, string text)
            {
                startTime = sTime;
                endTime = eTime;
                content = text;
            }
        }

        List<SubtitleEntry> subTitleLocal;

        //Dictionarty /HashMap whatever
        Dictionary<SubtitleNewLineOption, string> nlDict = new Dictionary<SubtitleNewLineOption, string>();

        public SubtitleConverter() {
            nlDict.Add(SubtitleNewLineOption.MacOLD, "\r");
            nlDict.Add(SubtitleNewLineOption.Unix, "\n");
            nlDict.Add(SubtitleNewLineOption.Windows, "\r\n");
        }
        //-------------------------------------------------------------------------Read Formats---------------//

        /// <summary>
        /// Converts an advanced substation alpha subtitle into the local subtitle format
        /// </summary>
        /// <param name="path">Path to the subtitle to read</param>
        private void ReadASS(string path)
        {
            string subContent = File.ReadAllText(path, EncodingRead);
            subContent = Regex.Replace(subContent, @"\{[^}]*\}", ""); //Remove all additional styling
            using (StringReader assFile = new StringReader(subContent)) 
            {
                string line = "";
                SState state = SState.Empty;
                while ((line = assFile.ReadLine()) != null) //Iterate over string
                {
                    switch(state)
                    {
                        case SState.Empty:
                            if (line.StartsWith("[Events]")) //The row before all dialog
                            {
                                assFile.ReadLine();          //Skip the format
                                state = SState.Iterating;
                            }
                            break;
                        case SState.Iterating:
                            if(line.StartsWith("Dialogue:"))       //As Diaglog starts with this
                            {
                                //Split into 10 Segments
                                //Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
                                //TODO: does it require them all?, or will the magic 10 be replace based on ^
                                string[] splitLine = line.Split(CommaArray, 10 , StringSplitOptions.None);
                                DateTime beginTime = DateTime.ParseExact(splitLine[1], "H:mm:ss.ff", CultureInfo.InvariantCulture);
                                DateTime endTime = DateTime.ParseExact(splitLine[2], "H:mm:ss.ff", CultureInfo.InvariantCulture);
                                string text2 = splitLine[9].Replace("\\N", "\n");//Replace \N with actual newlines
								string text = splitLine[3]+",,0,0,0,,"+text2;
                                subTitleLocal.Add(new SubtitleEntry(beginTime, endTime, text));
                            }
                            break;
                    }
                }
            }
            //Since ass/ssa can be in any order we must do this;
            //It shouldn't mess up already ordered for the next part
            subTitleLocal = subTitleLocal.OrderBy(o => o.startTime).ToList();
            JoinSameStart();
        }
        /// <summary>
        /// Reads a WebVTT to the applications local format
        /// </summary>
        /// <param name="path">The path to the subtitle to convert</param>
        private void ReadWebVTT(string path,string cssline, string rawf)
        {
            string raw = File.ReadAllText(path, EncodingRead);
            raw = raw.Replace("\r\n", "\n");    //Replace Windows format
            raw = raw.Replace("\r", "\n");      //Replace old Mac format (it's in the specs to do so)
            raw = raw.Trim();
			raw = raw.Replace("Subtitle-C", "ATCMain ");
			raw = raw.Replace("Caption-C", "ATCMain-Top ");
            rawf = raw;
            if (cssline != "")
            {
                string rawcss = File.ReadAllText(cssline, EncodingRead);
                rawcss = Regex.Replace(rawcss, @"cue \{(.*?)\}", "cue ");
                rawcss = rawcss.Replace(".rmp-container>.rmp-content>.rmp-cc-area>.rmp-cc-container>.rmp-cc-display>.rmp-cc-cue ", "\n");
                rawcss = rawcss.Replace(".Subtitle-C", "c.ATCMain ");
                rawcss = rawcss.Replace(".Caption-C", "c.ATCMain-Top "); //ADD c.ATC
                rawcss = Regex.Replace(rawcss, @"c.ATCMain-Top (\w+),", "\n");
                rawcss = Regex.Replace(rawcss, @"c.ATCMain (\w+),", "\n");
                rawcss = rawcss.Replace("font-size:.", "font-size:0.");
                rawcss = rawcss.Replace("color:AliceBlue", "color:#F0F8FF");
                rawcss = rawcss.Replace("color:antiquewhite", "color:#FAEBD7");
                rawcss = rawcss.Replace("color:aqua", "color:#00FFFF");
                rawcss = rawcss.Replace("color:aquamarine", "color:#7FFFD4");
                rawcss = rawcss.Replace("color:azure", "color:#F0FFFF");
                rawcss = rawcss.Replace("color:beige", "color:#F5F5DC");
                rawcss = rawcss.Replace("color:bisque", "color:#FFE4C4");
                rawcss = rawcss.Replace("color:black", "color:#000000");
                rawcss = rawcss.Replace("color:blanchedalmond", "color:#FFEBCD");
                rawcss = rawcss.Replace("color:blue", "color:#0000FF");
                rawcss = rawcss.Replace("color:blueviolet", "color:#8A2BE2");
                rawcss = rawcss.Replace("color:brown", "color:#A52A2A");
                rawcss = rawcss.Replace("color:burlywood", "color:#DEB887");
                rawcss = rawcss.Replace("color:cadetblue", "color:#5F9EA0");
                rawcss = rawcss.Replace("color:chartreuse", "color:#7FFF00");
                rawcss = rawcss.Replace("color:chocolate", "color:#D2691E");
                rawcss = rawcss.Replace("color:coral", "color:#FF7F50");
                rawcss = rawcss.Replace("color:cornflowerblue", "color:#6495ED");
                rawcss = rawcss.Replace("color:cornsilk", "color:#FFF8DC");
                rawcss = rawcss.Replace("color:crimson", "color:#DC143C");
                rawcss = rawcss.Replace("color:cyan", "color:#00FFFF");
                rawcss = rawcss.Replace("color:darkblue", "color:#00008B");
                rawcss = rawcss.Replace("color:darkcyan", "color:#008B8B");
                rawcss = rawcss.Replace("color:darkgoldenrod", "color:#B8860B");
                rawcss = rawcss.Replace("color:darkgray", "color:#A9A9A9");
                rawcss = rawcss.Replace("color:darkgreen", "color:#006400");
                rawcss = rawcss.Replace("color:darkkhaki", "color:#BDB76B");
                rawcss = rawcss.Replace("color:darkmagenta", "color:#8B008B");
                rawcss = rawcss.Replace("color:darkmagenta", "color:#8B008B");
                rawcss = rawcss.Replace("color:darkmagenta", "color:#8B008B");
                rawcss = rawcss.Replace("color:darkolivegreen", "color:#556B2F");
                rawcss = rawcss.Replace("color:darkorange", "color:#FF8C00");
                rawcss = rawcss.Replace("color:darkorchid", "color:#9932CC");
                rawcss = rawcss.Replace("color:darkred", "color:#8B0000");
                rawcss = rawcss.Replace("color:darksalmon", "color:#E9967A");
                rawcss = rawcss.Replace("color:darkseagreen", "color:#8FBC8F");
                rawcss = rawcss.Replace("color:darkslateblue", "color:#483D8B");
                rawcss = rawcss.Replace("color:darkslategray", "color:#2F4F4F");
                rawcss = rawcss.Replace("color:darkslategrey", "color:#2F4F4F");
                rawcss = rawcss.Replace("color:darkturquoise", "color:#00CED1");
                rawcss = rawcss.Replace("color:darkviolet", "color:#9400D3");
                rawcss = rawcss.Replace("color:deeppink", "color:#FF1493");
                rawcss = rawcss.Replace("color:deepskyblue", "color:#00BFFF");
                rawcss = rawcss.Replace("color:dimgray", "color:#696969");
                rawcss = rawcss.Replace("color:dimgrey", "color:#696969");
                rawcss = rawcss.Replace("color:dodgerblue", "color:#1E90FF");
                rawcss = rawcss.Replace("color:firebrick", "color:#B22222");
                rawcss = rawcss.Replace("color:floralwhite", "color:#FFFAF0");
                rawcss = rawcss.Replace("color:forestgreen", "color:#228B22");
                rawcss = rawcss.Replace("color:fuchsia", "color:#FF00FF");
                rawcss = rawcss.Replace("color:gainsboro", "color:#DCDCDC");
                rawcss = rawcss.Replace("color:ghostwhite", "color:#F8F8FF");
                rawcss = rawcss.Replace("color:gold", "color:#FFD700");
                rawcss = rawcss.Replace("color:goldenrod", "color:#DAA520");
                rawcss = rawcss.Replace("color:gray", "color:#808080");
                rawcss = rawcss.Replace("color:green", "color:#008000");
                rawcss = rawcss.Replace("color:greenyellow", "color:#ADFF2F");
                rawcss = rawcss.Replace("color:grey", "color:#808080");
                rawcss = rawcss.Replace("color:honeydew", "color:#F0FFF0");
                rawcss = rawcss.Replace("color:hotpink", "color:#FF69B4");
                rawcss = rawcss.Replace("color:indianred ", "color:#CD5C5C");
                rawcss = rawcss.Replace("color:indigo ", "color:#4B0082");
                rawcss = rawcss.Replace("color:ivory", "color:#FFFFF0");
                rawcss = rawcss.Replace("color:khaki", "color:#F0E68C");
                rawcss = rawcss.Replace("color:lavender", "color:#E6E6FA");
                rawcss = rawcss.Replace("color:lavenderblush", "color:#FFF0F5");
                rawcss = rawcss.Replace("color:lawngreen", "color:#7CFC00");
                rawcss = rawcss.Replace("color:lemonchiffon", "color:#FFFACD");
                rawcss = rawcss.Replace("color:lightblue", "color:#ADD8E6");
                rawcss = rawcss.Replace("color:lightcoral", "color:#F08080");
                rawcss = rawcss.Replace("color:lightcyan", "color:#E0FFFF");
                rawcss = rawcss.Replace("color:lightgoldenrodyellow", "color:#FAFAD2");
                rawcss = rawcss.Replace("color:lightgray", "color:#D3D3D3");
                rawcss = rawcss.Replace("color:lightgreen", "color:#90EE90");
                rawcss = rawcss.Replace("color:lightgrey", "color:#D3D3D3");
                rawcss = rawcss.Replace("color:lightpink", "color:#FFB6C1");
                rawcss = rawcss.Replace("color:lightsalmon", "color:#FFA07A");
                rawcss = rawcss.Replace("color:lightseagreen", "color:#20B2AA");
                rawcss = rawcss.Replace("color:lightskyblue", "color:#87CEFA");
                rawcss = rawcss.Replace("color:lightslategray", "color:#778899");
                rawcss = rawcss.Replace("color:lightslategrey", "color:#778899");
                rawcss = rawcss.Replace("color:lightsteelblue", "color:#B0C4DE");
                rawcss = rawcss.Replace("color:lightyellow", "color:#FFFFE0");
                rawcss = rawcss.Replace("color:lime", "color:#00FF00");
                rawcss = rawcss.Replace("color:limegreen", "color:#32CD32");
                rawcss = rawcss.Replace("color:linen", "color:#FAF0E6");
                rawcss = rawcss.Replace("color:magenta", "color:#FF00FF");
                rawcss = rawcss.Replace("color:maroon", "color:#800000");
                rawcss = rawcss.Replace("color:mediumaquamarine", "color:#66CDAA");
                rawcss = rawcss.Replace("color:mediumblue", "color:#0000CD");
                rawcss = rawcss.Replace("color:mediumorchid", "color:#BA55D3");
                rawcss = rawcss.Replace("color:mediumpurple", "color:#9370DB");
                rawcss = rawcss.Replace("color:mediumseagreen", "color:#3CB371");
                rawcss = rawcss.Replace("color:mediumslateblue", "color:#7B68EE");
                rawcss = rawcss.Replace("color:mediumspringgreen", "color:#00FA9A");
                rawcss = rawcss.Replace("color:mediumturquoise", "color:#48D1CC");
                rawcss = rawcss.Replace("color:mediumvioletred", "color:#C71585");
                rawcss = rawcss.Replace("color:midnightblue", "color:#191970");
                rawcss = rawcss.Replace("color:mintcream", "color:#F5FFFA");
                rawcss = rawcss.Replace("color:mistyrose", "color:#FFE4E1");
                rawcss = rawcss.Replace("color:moccasin", "color:#FFE4B5");
                rawcss = rawcss.Replace("color:navajowhite", "color:#FFDEAD");
                rawcss = rawcss.Replace("color:navy", "color:#000080");
                rawcss = rawcss.Replace("color:oldlace", "color:#FDF5E6");
                rawcss = rawcss.Replace("color:olive", "color:#808000");
                rawcss = rawcss.Replace("color:olivedrab", "color:#6B8E23");
                rawcss = rawcss.Replace("color:orange", "color:#FFA500");
                rawcss = rawcss.Replace("color:orangered", "color:#FF4500");
                rawcss = rawcss.Replace("color:orchid", "color:#DA70D6");
                rawcss = rawcss.Replace("color:palegoldenrod", "color:#EEE8AA");
                rawcss = rawcss.Replace("color:palegreen", "color:#98FB98");
                rawcss = rawcss.Replace("color:paleturquoise", "color:#AFEEEE");
                rawcss = rawcss.Replace("color:palevioletred", "color:#DB7093");
                rawcss = rawcss.Replace("color:papayawhip", "color:#FFEFD5");
                rawcss = rawcss.Replace("color:peachpuff", "color:#FFDAB9");
                rawcss = rawcss.Replace("color:peru", "color:#CD853F");
                rawcss = rawcss.Replace("color:pink", "color:#FFC0CB");
                rawcss = rawcss.Replace("color:plum", "color:#DDA0DD");
                rawcss = rawcss.Replace("color:powderblue", "color:#B0E0E6");
                rawcss = rawcss.Replace("color:purple", "color:#800080");
                rawcss = rawcss.Replace("color:rebeccapurple", "color:#663399");
                rawcss = rawcss.Replace("color:red", "color:#FF0000");
                rawcss = rawcss.Replace("color:rosybrown", "color:#BC8F8F");
                rawcss = rawcss.Replace("color:royalblue", "color:#4169E1");
                rawcss = rawcss.Replace("color:saddlebrown", "color:#8B4513");
                rawcss = rawcss.Replace("color:salmon", "color:#FA8072");
                rawcss = rawcss.Replace("color:sandybrown", "color:#F4A460");
                rawcss = rawcss.Replace("color:seagreen", "color:#2E8B57");
                rawcss = rawcss.Replace("color:seashell", "color:#FFF5EE");
                rawcss = rawcss.Replace("color:sienna", "color:#A0522D");
                rawcss = rawcss.Replace("color:silver", "color:#C0C0C0");
                rawcss = rawcss.Replace("color:skyblue", "color:#87CEEB");
                rawcss = rawcss.Replace("color:slateblue", "color:#6A5ACD");
                rawcss = rawcss.Replace("color:slategray", "color:#708090");
                rawcss = rawcss.Replace("color:slategrey", "color:#708090");
                rawcss = rawcss.Replace("color:snow", "color:#FFFAFA");
                rawcss = rawcss.Replace("color:springgreen", "color:#00FF7F");
                rawcss = rawcss.Replace("color:steelblue", "color:#4682B4");
                rawcss = rawcss.Replace("color:tan", "color:#D2B48C");
                rawcss = rawcss.Replace("color:teal", "color:#008080");
                rawcss = rawcss.Replace("color:thistle", "color:#D8BFD8");
                rawcss = rawcss.Replace("color:tomato", "color:#FF6347");
                rawcss = rawcss.Replace("color:turquoise", "color:#40E0D0");
                rawcss = rawcss.Replace("color:violet", "color:#EE82EE");
                rawcss = rawcss.Replace("color:wheat", "color:#F5DEB3");
                rawcss = rawcss.Replace("color:white", "color:#FFFFFF");
                rawcss = rawcss.Replace("color:whitesmoke", "color:#F5F5F5");
                rawcss = rawcss.Replace("color:yellow", "color:#FFFF00");
                rawcss = rawcss.Replace("color:yellowgreen", "color:#9ACD32");
                rawcss = Regex.Replace(rawcss, @"color:(\w+)", "color:#5D5D5D");
                rawcss = rawcss.Replace("\r\n", "\n");
                while (rawcss.Replace("\n\n", "\n") != rawcss) { rawcss = rawcss.Replace("\n\n", "\n"); }
                rawcss = rawcss.Trim();

                var splitedcss = rawcss.Split(NewLineArray, StringSplitOptions.None).ToList();
                int rawcsstam = rawcss.Split('\n').Length;
                for (int l = 0; l < rawcsstam; l++)
                {
                    
                    if (splitedcss[l].StartsWith("c."))
                    {
                        Regex getidcss1 = new Regex(@"c[^}]*_\d *");
                        Match getidcss0 = getidcss1.Match(splitedcss[l]);
                        Regex getidcss2 = new Regex(@"color:#(\w+)");
                        Match getidcsscor = getidcss2.Match(splitedcss[l]);
                        string getidcsscor00 = getidcsscor.Value.Replace("color:#","");
                        string getidcsscor01 = getidcsscor00.Remove(2);
                        string getidcsscor02 = getidcsscor00.Substring(2,2);
                        string getidcsscor03 = getidcsscor00.Substring(4);//convertercor

                        Regex getidcss3 = new Regex(@"font-size:[^e]+");
                        Match getidcsstam = getidcss3.Match(splitedcss[l]);
                        var csstfont1 = getidcsstam.Value.Replace("font-size:", "");
                        float valort1 = float.Parse(csstfont1.Replace(".",","));
                        float valort2 = valort1*60;
                        rawf = Regex.Replace(rawf, getidcss0.Value, getidcss0.Value + ">{\\c&H"+ getidcsscor03+ getidcsscor02+ getidcsscor01 + "&\\fs"+valort2+"}<");

                    }
                    else { Console.WriteLine("Deletado:" + splitedcss[l]); splitedcss[l] = ""; }}
            }
			Console.WriteLine("XD");
            raw = rawf;
            raw = Regex.Replace(raw, @"<[^>]*>", "");
            var splited = raw.Split(NewLineArray, StringSplitOptions.None).ToList();
            SState ss = SState.Empty; //Current state
            if (!splited[0].StartsWith("WEBVTT")) return; //Not a valid WebVTT
            DateTime beginTime = new DateTime();
            DateTime endTime = new DateTime();
            string textContent = "";
            string taxn1 = "960";
            string taxn2 = "";
            foreach (string line in splited.Skip(2)) //Iterate line by line
            {
                switch (ss)
                {
                    case (SState.Empty):
                        string linetrim = line.TrimEnd();
                        Regex max2 = new Regex(@"position:([0-9]*)");
                        Match max3 = max2.Match(linetrim);
                        Regex max4 = new Regex(@"line:([0-9]*)");
                        Match max5 = max4.Match(linetrim);

                        if (max3.Value == "") { taxn1 = "960"; }
                        if (max3.Value == "position:01") { taxn1 = "19"; }
                        if (max3.Value == "position:02") { taxn1 = "38"; }
                        if (max3.Value == "position:03") { taxn1 = "58"; }
                        if (max3.Value == "position:04") { taxn1 = "77"; }
                        if (max3.Value == "position:05") { taxn1 = "96"; }
                        if (max3.Value == "position:06") { taxn1 = "115"; }
                        if (max3.Value == "position:07") { taxn1 = "134"; }
                        if (max3.Value == "position:08") { taxn1 = "154"; }
                        if (max3.Value == "position:09") { taxn1 = "173"; }
                        if (max3.Value == "position:10") { taxn1 = "192"; }
                        if (max3.Value == "position:11") { taxn1 = "211"; }
                        if (max3.Value == "position:12") { taxn1 = "230"; }
                        if (max3.Value == "position:13") { taxn1 = "250"; }
                        if (max3.Value == "position:14") { taxn1 = "269"; }
                        if (max3.Value == "position:15") { taxn1 = "288"; }
                        if (max3.Value == "position:16") { taxn1 = "307"; }
                        if (max3.Value == "position:17") { taxn1 = "326"; }
                        if (max3.Value == "position:18") { taxn1 = "346"; }
                        if (max3.Value == "position:19") { taxn1 = "365"; }
                        if (max3.Value == "position:20") { taxn1 = "384"; }
                        if (max3.Value == "position:21") { taxn1 = "403"; }
                        if (max3.Value == "position:22") { taxn1 = "422"; }
                        if (max3.Value == "position:23") { taxn1 = "442"; }
                        if (max3.Value == "position:24") { taxn1 = "461"; }
                        if (max3.Value == "position:25") { taxn1 = "480"; }
                        if (max3.Value == "position:26") { taxn1 = "499"; }
                        if (max3.Value == "position:27") { taxn1 = "518"; }
                        if (max3.Value == "position:28") { taxn1 = "538"; }
                        if (max3.Value == "position:29") { taxn1 = "557"; }
                        if (max3.Value == "position:30") { taxn1 = "576"; }
                        if (max3.Value == "position:31") { taxn1 = "595"; }
                        if (max3.Value == "position:32") { taxn1 = "614"; }
                        if (max3.Value == "position:33") { taxn1 = "634"; }
                        if (max3.Value == "position:34") { taxn1 = "653"; }
                        if (max3.Value == "position:35") { taxn1 = "672"; }
                        if (max3.Value == "position:36") { taxn1 = "691"; }
                        if (max3.Value == "position:37") { taxn1 = "710"; }
                        if (max3.Value == "position:38") { taxn1 = "730"; }
                        if (max3.Value == "position:39") { taxn1 = "749"; }
                        if (max3.Value == "position:40") { taxn1 = "768"; }
                        if (max3.Value == "position:41") { taxn1 = "787"; }
                        if (max3.Value == "position:42") { taxn1 = "806"; }
                        if (max3.Value == "position:43") { taxn1 = "826"; }
                        if (max3.Value == "position:44") { taxn1 = "845"; }
                        if (max3.Value == "position:45") { taxn1 = "864"; }
                        if (max3.Value == "position:46") { taxn1 = "883"; }
                        if (max3.Value == "position:47") { taxn1 = "902"; }
                        if (max3.Value == "position:48") { taxn1 = "922"; }
                        if (max3.Value == "position:49") { taxn1 = "941"; }
                        if (max3.Value == "position:50") { taxn1 = "960"; }
                        if (max3.Value == "position:51") { taxn1 = "979"; }
                        if (max3.Value == "position:52") { taxn1 = "998"; }
                        if (max3.Value == "position:53") { taxn1 = "1018"; }
                        if (max3.Value == "position:54") { taxn1 = "1037"; }
                        if (max3.Value == "position:55") { taxn1 = "1056"; }
                        if (max3.Value == "position:56") { taxn1 = "1075"; }
                        if (max3.Value == "position:57") { taxn1 = "1094"; }
                        if (max3.Value == "position:58") { taxn1 = "1114"; }
                        if (max3.Value == "position:59") { taxn1 = "1133"; }
                        if (max3.Value == "position:60") { taxn1 = "1152"; }
                        if (max3.Value == "position:61") { taxn1 = "1171"; }
                        if (max3.Value == "position:62") { taxn1 = "1190"; }
                        if (max3.Value == "position:63") { taxn1 = "1210"; }
                        if (max3.Value == "position:64") { taxn1 = "1229"; }
                        if (max3.Value == "position:65") { taxn1 = "1248"; }
                        if (max3.Value == "position:66") { taxn1 = "1267"; }
                        if (max3.Value == "position:67") { taxn1 = "1286"; }
                        if (max3.Value == "position:68") { taxn1 = "1306"; }
                        if (max3.Value == "position:69") { taxn1 = "1325"; }
                        if (max3.Value == "position:70") { taxn1 = "1344"; }
                        if (max3.Value == "position:71") { taxn1 = "1363"; }
                        if (max3.Value == "position:72") { taxn1 = "1382"; }
                        if (max3.Value == "position:73") { taxn1 = "1402"; }
                        if (max3.Value == "position:74") { taxn1 = "1421"; }
                        if (max3.Value == "position:75") { taxn1 = "1440"; }
                        if (max3.Value == "position:76") { taxn1 = "1459"; }
                        if (max3.Value == "position:77") { taxn1 = "1478"; }
                        if (max3.Value == "position:78") { taxn1 = "1498"; }
                        if (max3.Value == "position:79") { taxn1 = "1517"; }
                        if (max3.Value == "position:80") { taxn1 = "1536"; }
                        if (max3.Value == "position:81") { taxn1 = "1555"; }
                        if (max3.Value == "position:82") { taxn1 = "1574"; }
                        if (max3.Value == "position:83") { taxn1 = "1594"; }
                        if (max3.Value == "position:84") { taxn1 = "1613"; }
                        if (max3.Value == "position:85") { taxn1 = "1632"; }
                        if (max3.Value == "position:86") { taxn1 = "1651"; }
                        if (max3.Value == "position:87") { taxn1 = "1670"; }
                        if (max3.Value == "position:88") { taxn1 = "1690"; }
                        if (max3.Value == "position:89") { taxn1 = "1709"; }
                        if (max3.Value == "position:90") { taxn1 = "1728"; }
                        if (max3.Value == "position:91") { taxn1 = "1747"; }
                        if (max3.Value == "position:92") { taxn1 = "1766"; }
                        if (max3.Value == "position:93") { taxn1 = "1786"; }
                        if (max3.Value == "position:94") { taxn1 = "1805"; }
                        if (max3.Value == "position:95") { taxn1 = "1824"; }
                        if (max3.Value == "position:96") { taxn1 = "1843"; }
                        if (max3.Value == "position:97") { taxn1 = "1862"; }
                        if (max3.Value == "position:98") { taxn1 = "1882"; }
                        if (max3.Value == "position:99") { taxn1 = "1901"; }
                        if (max3.Value == "position:100") { taxn1 = "1920"; }

                        if (max5.Value == "") { taxn1 = ""; }
                        if (max5.Value == "line:01") { taxn2 = "61"; }
                        if (max5.Value == "line:02") { taxn2 = "72"; }
                        if (max5.Value == "line:03") { taxn2 = "82"; }
                        if (max5.Value == "line:04") { taxn2 = "93"; }
                        if (max5.Value == "line:05") { taxn2 = "104"; }
                        if (max5.Value == "line:06") { taxn2 = "115"; }
                        if (max5.Value == "line:07") { taxn2 = "126"; }
                        if (max5.Value == "line:08") { taxn2 = "136"; }
                        if (max5.Value == "line:09") { taxn2 = "147"; }
                        if (max5.Value == "line:10") { taxn2 = "158"; }
                        if (max5.Value == "line:11") { taxn2 = "169"; }
                        if (max5.Value == "line:12") { taxn2 = "180"; }
                        if (max5.Value == "line:13") { taxn2 = "190"; }
                        if (max5.Value == "line:14") { taxn2 = "200"; }
                        if (max5.Value == "line:15") { taxn2 = "210"; }
                        if (max5.Value == "line:16") { taxn2 = "220"; }
                        if (max5.Value == "line:17") { taxn2 = "230"; }
                        if (max5.Value == "line:18") { taxn2 = "244"; }
                        if (max5.Value == "line:19") { taxn2 = "255"; }
                        if (max5.Value == "line:20") { taxn2 = "266"; }
                        if (max5.Value == "line:21") { taxn2 = "277"; }
                        if (max5.Value == "line:22") { taxn2 = "288"; }
                        if (max5.Value == "line:23") { taxn2 = "298"; }
                        if (max5.Value == "line:24") { taxn2 = "309"; }
                        if (max5.Value == "line:25") { taxn2 = "320"; }
                        if (max5.Value == "line:26") { taxn2 = "331"; }
                        if (max5.Value == "line:27") { taxn2 = "342"; }
                        if (max5.Value == "line:28") { taxn2 = "352"; }
                        if (max5.Value == "line:29") { taxn2 = "363"; }
                        if (max5.Value == "line:30") { taxn2 = "374"; }
                        if (max5.Value == "line:31") { taxn2 = "385"; }
                        if (max5.Value == "line:32") { taxn2 = "396"; }
                        if (max5.Value == "line:33") { taxn2 = "406"; }
                        if (max5.Value == "line:34") { taxn2 = "417"; }
                        if (max5.Value == "line:35") { taxn2 = "428"; }
                        if (max5.Value == "line:36") { taxn2 = "439"; }
                        if (max5.Value == "line:37") { taxn2 = "450"; }
                        if (max5.Value == "line:38") { taxn2 = "460"; }
                        if (max5.Value == "line:39") { taxn2 = "471"; }
                        if (max5.Value == "line:40") { taxn2 = "482"; }
                        if (max5.Value == "line:41") { taxn2 = "493"; }
                        if (max5.Value == "line:42") { taxn2 = "504"; }
                        if (max5.Value == "line:43") { taxn2 = "514"; }
                        if (max5.Value == "line:44") { taxn2 = "525"; }
                        if (max5.Value == "line:45") { taxn2 = "536"; }
                        if (max5.Value == "line:46") { taxn2 = "547"; }
                        if (max5.Value == "line:47") { taxn2 = "558"; }
                        if (max5.Value == "line:48") { taxn2 = "568"; }
                        if (max5.Value == "line:49") { taxn2 = "579"; }
                        if (max5.Value == "line:50") { taxn2 = "590"; }
                        if (max5.Value == "line:51") { taxn2 = "601"; }
                        if (max5.Value == "line:52") { taxn2 = "612"; }
                        if (max5.Value == "line:53") { taxn2 = "622"; }
                        if (max5.Value == "line:54") { taxn2 = "633"; }
                        if (max5.Value == "line:55") { taxn2 = "644"; }
                        if (max5.Value == "line:56") { taxn2 = "655"; }
                        if (max5.Value == "line:57") { taxn2 = "666"; }
                        if (max5.Value == "line:58") { taxn2 = "676"; }
                        if (max5.Value == "line:59") { taxn2 = "687"; }
                        if (max5.Value == "line:60") { taxn2 = "698"; }
                        if (max5.Value == "line:61") { taxn2 = "709"; }
                        if (max5.Value == "line:62") { taxn2 = "720"; }
                        if (max5.Value == "line:63") { taxn2 = "730"; }
                        if (max5.Value == "line:64") { taxn2 = "741"; }
                        if (max5.Value == "line:65") { taxn2 = "752"; }
                        if (max5.Value == "line:66") { taxn2 = "763"; }
                        if (max5.Value == "line:67") { taxn2 = "774"; }
                        if (max5.Value == "line:68") { taxn2 = "784"; }
                        if (max5.Value == "line:69") { taxn2 = "795"; }
                        if (max5.Value == "line:70") { taxn2 = "806"; }
                        if (max5.Value == "line:71") { taxn2 = "817"; }
                        if (max5.Value == "line:72") { taxn2 = "828"; }
                        if (max5.Value == "line:73") { taxn2 = "838"; }
                        if (max5.Value == "line:74") { taxn2 = "849"; }
                        if (max5.Value == "line:75") { taxn2 = "860"; }
                        if (max5.Value == "line:76") { taxn2 = "891"; }
                        if (max5.Value == "line:77") { taxn2 = "942"; }
                        if (max5.Value == "line:78") { taxn2 = "908"; }
                        if (max5.Value == "line:79") { taxn2 = "918"; }
                        if (max5.Value == "line:80") { taxn2 = "930"; }
                        if (max5.Value == "line:81") { taxn2 = "945"; }
                        if (max5.Value == "line:82") { taxn2 = "956"; }
                        if (max5.Value == "line:83") { taxn2 = "966"; }
                        if (max5.Value == "line:84") { taxn2 = "1017"; }
                        if (max5.Value == "line:85") { taxn2 = "988"; }
                        if (max5.Value == "line:86") { taxn2 = "999"; }
                        if (max5.Value == "line:87") { taxn2 = "1028"; }
                        if (max5.Value == "line:88") { taxn2 = "1038"; }
                        if (max5.Value == "line:89") { taxn2 = "1044"; }
                        if (max5.Value == "line:90") { taxn2 = "1050"; }
                        if (max5.Value == "line:91") { taxn2 = "1055"; }
                        if (max5.Value == "line:92") { taxn2 = "1060"; }
                        if (max5.Value == "line:93") { taxn2 = "1068"; }
                        if (max5.Value == "line:94") { taxn2 = "1070"; }
                        if (max5.Value == "line:95") { taxn2 = "1074"; }
                        if (max5.Value == "line:96") { taxn2 = "1076"; }
                        if (max5.Value == "line:97") { taxn2 = "1077"; }
                        if (max5.Value == "line:98") { taxn2 = "1078"; }
                        if (max5.Value == "line:99") { taxn2 = "1079"; }
                        if (max5.Value == "line:100") { taxn2 = "1080"; }

                        //Console.WriteLine();

                        string[] max0 = linetrim.Split(' ');
                        string[] max1 = Regex.Split(max0[0], @".\d\W");
                        if (linetrim.Equals("")) continue;
                        textContent += max1[0] + "";

                        if (subTitleLocal.Count == 0 && linetrim.Equals("STYLE") || linetrim.StartsWith("STYLE "))
                        {
                            ss = SState.Comment;
                            break;
                        }
                        //WEBVTTComment, or region values we'll just skip
                        if (linetrim.Equals("NOTE") || linetrim.StartsWith("NOTE ") || linetrim.Equals("REGION"))
                        {
                            ss = SState.Comment;
                            break;
                        }
                        if (linetrim.Contains("-->")) goto case (SState.Timestamp); //As we dont care for Queue ID, test only for timestamp
                        break;

                    case (SState.Timestamp):
                        
                        //Split and parse the timestamp 
                        string[] time = Regex.Split(line, " *--> *");
                        //Parse the time, can only be one of 2 option, so try this one first
                        bool tryBegin = DateTime.TryParseExact(time[0], "HH:mm:ss.fff", CultureInfo.InvariantCulture, DateTimeStyles.None, out beginTime);
                        string[] endTimeSplit = time[1].Split(SpaceArray, StringSplitOptions.None); //Timestamp might contain info after it, so remove it
                        bool tryEnd = DateTime.TryParseExact(endTimeSplit[0], "HH:mm:ss.fff", CultureInfo.InvariantCulture, DateTimeStyles.None, out endTime);
                        //If something went wrong, parse it differnetly;
                        if (!tryBegin) DateTime.TryParseExact(time[0], "mm:ss.fff", CultureInfo.InvariantCulture, DateTimeStyles.None, out beginTime);
                        if (!tryEnd) DateTime.TryParseExact(endTimeSplit[0], "mm:ss.fff", CultureInfo.InvariantCulture, DateTimeStyles.None, out endTime);
                        ss = SState.Iterating;
                        break;

                    case (SState.Iterating):
                        if (line.Equals("")) //Come to the end of the cue block so add it to the sub
                        {
                            string cleanedString = textContent.TrimEnd(); //Remove the additional newline we added
                            subTitleLocal.Add(new SubtitleEntry(beginTime, endTime, cleanedString));
                            textContent = "";                             //Cleanup
                            ss = SState.Empty;
                        }                        
                        else textContent += ",,0,0,0,,{\\pos("+taxn1+","+taxn2+")}" + line + "\n";
						break;

                    case (SState.Comment): //We dont want notes so lets go here
                        string linetrimc = line.TrimEnd();
                        if (linetrimc.Equals("")) ss = SState.Empty;//Reached the end of the comment/style/region;
                        break;
                }
            }
            if (ss == SState.Iterating) //End of file, add last if we were still going
            {
                string cleanedString = textContent.TrimEnd(); //Remove the additional newline we added
                subTitleLocal.Add(new SubtitleEntry(beginTime, endTime, cleanedString));
            }
        }
        //-------------------------------------------------------------------------Write Formats---------------//
        /// <summary>
        /// Writes the current subtitle stored to the advanced subtitle station alpha
        /// </summary>
        /// <param name="path">The location to save to</param>
        private void WriteASS(string path)
        {
            string nlASS = GetNewlineType("\n");
            string head = "[Script Info]"      + nlASS +
                          "Title: Maxwgod"  + nlASS +
                          "ScriptType: v4.00+" + nlASS +
                          "Collisions: Normal" + nlASS +
                          "PlayDepth: 0" + nlASS +
                          "PlayResX: 1920" + nlASS +
                          "PlayResY: 1080" + nlASS + nlASS;

            string styles = "[v4+ Styles]" + nlASS +
                            "Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding" + nlASS +
                            //"Style: maxwgod01,GillSans,60,&H00FFFFFF,&H0300FFFF,&H00000000,&H02000000,-1,0,0,0,100,100,0,0,1,2,1,2,40,40,50,1" + nlASS +
                            //"Style: maxwgod02,GillSans,40,&H00FFFFFF,&H0300FFFF,&H00000000,&H02000000,-1,0,0,0,100,100,0,0,1,2,1,4,40,40,50,1\n\n";
                            "Style: ATCMain,GillSans,60,&H00FFFFFF,&H0300FFFF,&H00000000,&H02000000,-1,0,0,0,100,100,0,0,1,2,1,2,50,50,50,1" + nlASS +
                            "Style: ATCMain-Top,GillSans,60,&H00FFFFFF,&H0300FFFF,&H00000000,&H02000000,-1,0,0,0,100,100,0,0,1,2,1,4,50,50,50,1\n\n";
            string events = "[Events]" + nlASS +
                           "Format: Layer, Start, End, Style, Actor, MarginL, MarginR, MarginV, Effect, Text" + nlASS;
            StringBuilder builder = new StringBuilder();
            builder.Append(head);
            builder.Append(styles);
            builder.Append(events);
            foreach(SubtitleEntry entry in subTitleLocal)
            {
                string startTime = entry.startTime.ToString("H:mm:ss.ff");
                string endTime = entry.endTime.ToString("H:mm:ss.ff");
                builder.Append(string.Format("Dialogue: 0,{0},{1},{2}" + nlASS, startTime, endTime, entry.content.Replace("J\n", "\\N")));
            }
            File.WriteAllText(path, builder.ToString());
        }

        /// <summary>
        /// Converts the local format to Subrip format
        /// </summary>
        /// <param name="path">Output path for subtitle</param>
        private void WriteSRT(string path)
        {
            string nlSRT = GetNewlineType("\n");
            StringBuilder subExport = new StringBuilder();
            int i = 0;
            foreach (SubtitleEntry entry in subTitleLocal)
            {
                i++;
                string sTime = entry.startTime.ToString("HH:mm:ss,fff");
                string eTime = entry.endTime.ToString("HH:mm:ss,fff");
                subExport.Append(i+ nlSRT + sTime + " --> " + eTime + nlSRT + entry.content + nlSRT + nlSRT);
            }
            File.WriteAllText(path, subExport.ToString());
        }
        //--------------------------------------------Misc stuff -----------------//
        /// <summary>
        ///Remove dupicale start times and join to one
        ///Assumes the subs are sorted 
        ///Taken from and modified from http://stackoverflow.com/questions/14918668/find-duplicates-and-merge-items-in-a-list  
        /// </summary>
        private void JoinSameStart()
        {
            for (int i = 0; i < subTitleLocal.Count - 1; i++)
            {
                var item = subTitleLocal[i];
                for (int j = i + 1; j < subTitleLocal.Count; )
                {
                    var anotherItem = subTitleLocal[j];
                    if (item.startTime > anotherItem.startTime) break; //No point contiuning as the list is sorted
                    if (item.startTime.Equals(anotherItem.startTime))
                    {
                        string[] textfi = Regex.Split(anotherItem.content, ",,");
                        item.content = item.content + "\\N" + textfi[2];
                        subTitleLocal.RemoveAt(j);
                    }
                    else

                        j++;
                }
            }
        }

        /// <summary>
        /// Parses a timemetric ie 12h31m2s44ms
        /// </summary>
        /// <param name="metric">The metric string</param>
        /// <returns>The Timespan equivilent</returns>
        private TimeSpan ParseTimeMetricTimeSpan(string metric)
        {
            TimeSpan time = TimeSpan.Zero;
            Regex rg = new Regex(@"([0-9.]+)([a-z]+)");
            MatchCollection mtchs = rg.Matches(metric);
            foreach (Match match in mtchs)
            {
                float st = float.Parse(match.Groups[1].Value);
                switch (match.Groups[2].Value)
                {
                    case ("h"):
                        time = time.Add(TimeSpan.FromHours(st));
                        break;
                    case ("m"):
                        time = time.Add(TimeSpan.FromMinutes(st));
                        break;
                    case ("s"):
                        time = time.Add(TimeSpan.FromSeconds(st));
                        break;
                    case ("ms"):
                        time = time.Add(TimeSpan.FromMilliseconds(st));
                        break;
                }
            }
            return time;
        }

        /// <summary>
        /// Gets the newline type
        /// </summary>
        /// <param name="defaultValue">The default newline value if its not set</param>
        /// <returns>The newline option</returns>
        private string GetNewlineType(string defaultValue)
        {
            if (subtitleNewLineOption != SubtitleNewLineOption.Default)
            {
                return nlDict[subtitleNewLineOption];
            }
            return defaultValue;
        }

        /// <summary>
        /// Read a subtitle from the specified input path / extension
        /// </summary>
        /// <param name="input">Path to the subtitle</param>
        /// <returns>A boolean representing the success of the operation</returns>
        public bool ReadSubtitle(string input,string cssline)
        {
            subTitleLocal = new List<SubtitleEntry>();
            string extensionInput = Path.GetExtension(input).ToLower();
            string rawf = "maxwgod";
            switch (extensionInput) //Read file
            {
                case (".vtt"):
                    ReadWebVTT(input,cssline,rawf);
                    break;
				case (".ass"):
                case (".ssa"):
                    ReadASS(input);
                    break;
                default:
                    Console.WriteLine("Invalid input format VTT Hidive");
                    return false;
            }
            return true;
        }
        /// <summary>
        /// Writes a subtitle to the specified output path / extension
        /// </summary>
        /// <param name="output">Output path location with file extension</param>
        /// <returns>A boolean representing the success of the operation</returns>
        public bool WriteSubtitle(string output)
        {
            string extensionOutput = Path.GetExtension(output).ToLower();

            switch (extensionOutput) //Write to file
            {
                case (".ass"):
                case (".ssa"):
                    WriteASS(output);
                    break;
                case (".srt"):
                    WriteSRT(output);
                    break;
                default:
                    Console.WriteLine("Invalid write format ASS/SRT");
                    return false;
            }
            return true;
        }
        /// <summary>
        /// Convert a subtitle, supports specififed by the input and output file extension
        /// ASS/SSA DFXP/TTML, SUB, SRT, WSRT, VTT;
        /// </summary>
        /// <param name="input">The path to the subtitle to convert</param>
        /// <param name="output">The path to the location to save, and file name/type to convert to</param>
        /// <returns>A boolean representing the success of the operation</returns>
        public bool ConvertSubtitle(string input, string output)
        {
            return ConvertSubtitle(input, output, "");
        }
        /// <summary>
        /// Convert a subtitle, supports specififed by the input and output file extension
        /// ASS/SSA DFXP/TTML, SUB, SRT, WSRT, VTT;
        /// </summary>
        /// <param name="input">The path to the subtitle to convert</param>
        /// <param name="output">The path to the location to save, and file name/type to convert to</param>
        /// <param name="cssline"> The time to shift the subtitle</param>
        /// <returns>A boolean representing the success of the operation</returns>
        public bool ConvertSubtitle(string input, string output, string cssline)
        {
            if (!ReadSubtitle(input,cssline)) return false;
            if (!WriteSubtitle(output)) return false;
            return true;
        }
    }
}
