using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HtmlAgilityPack;
using System.IO;
using System.Xml.Serialization;
using System.Xml;

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public ObservableCollection<Result> results = new ObservableCollection<Result>();
        public ObservableCollection<Result> Results { get { return results; } }
        HashSet<int> idSet = new HashSet<int>();

        public MainWindow()
        {
            InitializeComponent();

            StreamReader reader = new StreamReader("test.xml");
            results = Deserialize<ObservableCollection<Result>>(reader.ReadToEnd());
            reader.Close();
            foreach(var res in results)
            {
                idSet.Add(res.Id);
            }
            var lastResultId = 7067;

            if (results.Count > 0)
                lastResultId = results[results.Count - 1].Id;

            using (WebClient client = new WebClient())
            {
                for (int i = 1; i < 1000; i++)
                {

                    var webRequest = HttpWebRequest.Create(String.Format("https://www.eestiloto.ee/osi/stats.do?ldate=&gameCode=12&byNum=Otsi&sort=0&unum=&action=searchDraws&udate=&lnum={1}&pageNum={0}", i,lastResultId));
                    Stream stream = webRequest.GetResponse().GetResponseStream();

                    var doc = new HtmlAgilityPack.HtmlDocument();
                    HtmlAgilityPack.HtmlNode.ElementsFlags["br"] = HtmlAgilityPack.HtmlElementFlag.Empty;
                    doc.OptionWriteEmptyNodes = true;
                    doc.Load(stream);


                    string testDivSelector = "//*[@class=\"cart-keno\"]";
                    var nodes = doc.DocumentNode.SelectNodes(testDivSelector);
                    if (nodes != null)
                    {
                        foreach (var node in nodes)
                        {
                            var idstr = node.SelectSingleNode("td[1]").InnerHtml.ToString();
                            var id = Convert.ToInt32(idstr);
                            if (idSet.Contains(id))
                                continue;
                            var result = node.SelectSingleNode("td[3]").InnerHtml.ToString();
                            var resultstr = result.Split(' ');
                            var resultArr = new int[resultstr.Length];
                            for (int j = 0; j < resultArr.Length; j++)
                            {
                                resultArr[j] = Convert.ToInt32(resultstr[j]);
                            }
                            var resultClass = new Result() { Id = id, Results = resultArr };
                            results.Add(resultClass);
                        }
                    }
                    if (nodes == null)
                        break;
                }
                var stringWriter = Serialize(results);
                var xml = stringWriter.ToString();
                StreamWriter writer = new StreamWriter("test.xml");
                writer.Write(xml);
                writer.Flush();
                writer.Close();
            }

        }

        public static T Deserialize<T>(string xml)
        {
            var xs = new XmlSerializer(typeof(T));
            return (T)xs.Deserialize(new StringReader(xml));
        }

        public static StringWriter Serialize(object o)
        {
            var xs = new XmlSerializer(o.GetType());
            var xml = new StringWriter();
            xs.Serialize(xml, o);

            return xml;
        }
    }
}
