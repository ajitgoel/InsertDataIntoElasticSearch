using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Configuration;

using HtmlAgilityPack;
using NLog;
using Nest;
    
namespace InsertDataIntoElasticSearch
{
    using System.Threading.Tasks;

    using Timer = System.Threading.Timer;

    class Program
    {
        private static void DoWork(object source)
        {
            try
            {
                var folderLocationsAll = ConfigurationManager.AppSettings["EOL-Folders"];
                var folderLocations = folderLocationsAll.Split(';');

                //var ElasticSearchServerURI = "http://ipv4.fiddler:9200";
                var ElasticSearchServerURI = "http://agoel-dt:9200";
            
                var defaultIndex = "EOL".ToLower();
                var uri = new Uri(ElasticSearchServerURI);
                var connectionSettingsForIndexingFiles = ConnectionSettingsForIndexingFiles(defaultIndex, uri);
                var elasticClientForIndexingFiles = ElasticClientForIndexingFiles(connectionSettingsForIndexingFiles);

                foreach (var folderLocation in folderLocations)
                {
                    if (HasFolderBeenIndexed(folderLocation) == false)
                    {
                        var files = GetAllFilesInFolder(folderLocation, "*.xml");
                        var errors = ParseFiles(files);
                        WriteErrorsIntoElasticSearchIndex(elasticClientForIndexingFiles, errors);
                        SaveFolderHasBeenIndexed(folderLocation);
                    }
                }
            }
            catch (Exception exception)
            {
                LogManager.GetCurrentClassLogger().Fatal(exception.ToString());
            }
            GC.Collect();
        }

        static void Main(string[] args)
        {
            var timer = new Timer(DoWork, null, 0, 60 * 60 * 1000);
            Console.ReadLine();
        }

        private static bool HasFolderBeenIndexed(string folderLocation)
        {
            var indexedFolders = File.ReadAllLines(IndexedFolders_FileName());
            if(indexedFolders.Any(folderLocation.Contains))
            {
                return true;
            }
            return false;
        }

        private static void SaveFolderHasBeenIndexed(string folderLocation)
        {
            File.AppendAllText(IndexedFolders_FileName(), folderLocation + Environment.NewLine);
        }
        
        private static string IndexedFolders_FileName()
        {
            var fileName = String.Format("{0}\\{1}", AppDomain.CurrentDomain.BaseDirectory, "IndexedFolders_FileName.txt");
            return fileName;
        }

        private static ElasticClient ElasticClientForIndexingFiles(
            IConnectionSettingsValues connectionSettingsForIndexingFiles)
        {
            var elasticClientForIndexingFiles = 
                new ElasticClient(connectionSettingsForIndexingFiles);
            return elasticClientForIndexingFiles;
        }

        private static ConnectionSettings ConnectionSettingsForIndexingFiles(
            string defaultIndex, Uri uri)
        {
            var connectionSettingsForIndexingFiles =
                new ConnectionSettings(uri, defaultIndex).EnableTrace().
                ExposeRawResponse();
            return connectionSettingsForIndexingFiles;
        }

        private static void WriteErrorsIntoElasticSearchIndex(ElasticClient elasticClient, List<error> errors)
        {
            foreach (var error in errors)
            {
                elasticClient.Index(error);    
            }
        }

        static IEnumerable<string> GetAllFilesInFolder(string path, string searchPattern)
        {
            var files = Directory.GetFiles(path, searchPattern, SearchOption.AllDirectories).ToArray();
            return files;
        }

        private static List<error> ParseFiles(IEnumerable<string> files)
        {
            var errors = new List<error>();

            try
            {
                Parallel.ForEach(files, file =>
                {
                //foreach (var file in files)
                //{
                    #region parse Files
                    var htmlDocument = new HtmlDocument();
                    htmlDocument.Load(file);

                    var xElementErrorNode = htmlDocument.DocumentNode.SelectSingleNode("error");
                    var serverVariablesChildNode = xElementErrorNode.ChildNodes.Count > 1
                                                        ? xElementErrorNode.ChildNodes[1]
                                                        : null;
                    var sessionVariablesChildNode = xElementErrorNode.ChildNodes.Count > 3
                                                        ? xElementErrorNode.ChildNodes[3]
                                                        : null;
                    var queryStringChildNode = xElementErrorNode.ChildNodes.Count > 5
                                                    ? xElementErrorNode.ChildNodes[5]
                                                    : null;
                    var dataChildNode = xElementErrorNode.ChildNodes.Count > 9 ? xElementErrorNode.ChildNodes[9] : null;

                    var xElementPathTranslatedNode = serverVariablesChildNode == null
                                                            ? null
                                                            : serverVariablesChildNode.SelectSingleNode(
                                                                "item[@name='PATH_TRANSLATED']");
                    var xElementHttpSoapActionNode = serverVariablesChildNode == null
                                                            ? null
                                                            : serverVariablesChildNode.SelectSingleNode(
                                                                "item[@name='HTTP_SOAPACTION']");
                    var xElementHttpRefererNode = serverVariablesChildNode == null
                                                        ? null
                                                        : serverVariablesChildNode.SelectSingleNode(
                                                            "item[@name='HTTP_REFERER']");
                    var xElementQueryNode = serverVariablesChildNode == null
                                                ? null
                                                : serverVariablesChildNode.SelectSingleNode("item[@name='Query']");
                    var xElementHTTPASPFILTERSESSIONIDNode = serverVariablesChildNode == null
                                                                    ? null
                                                                    : serverVariablesChildNode.SelectSingleNode(
                                                                        "item[@name='HTTP_ASPFILTERSESSIONID']");
                    var xElementQueryString = serverVariablesChildNode == null
                                                    ? null
                                                    : serverVariablesChildNode.SelectSingleNode(
                                                        "item[@name='QUERY_STRING']");
                    var xElementHTTPClientSrcIP = serverVariablesChildNode == null
                                                        ? null
                                                        : serverVariablesChildNode.SelectSingleNode(
                                                            "item[@name='HTTP_CLIENT_SRC_IP']");

                    var xElementStateOfPerson = dataChildNode == null
                                                    ? null
                                                    : dataChildNode.SelectSingleNode("item[@name='StateOfPerson']");

                    var xElementSiteId = queryStringChildNode == null
                                                ? null
                                                : queryStringChildNode.SelectSingleNode("item[@name='siteId']");
                    var xElementEOLUserName = sessionVariablesChildNode == null
                                                    ? null
                                                    : sessionVariablesChildNode.SelectSingleNode(
                                                        "item[@name='EOLUserName']");
                    var xElementModel = queryStringChildNode == null
                                            ? null
                                            : queryStringChildNode.SelectSingleNode("item[@name='model']");

                    var host = xElementErrorNode.Attributes["host"] == null
                                    ? string.Empty
                                    : xElementErrorNode.Attributes["host"].Value;
                    var errorType = xElementErrorNode.Attributes["type"] == null
                                        ? string.Empty
                                        : xElementErrorNode.Attributes["type"].Value;
                    var message = xElementErrorNode.Attributes["message"] == null
                                        ? string.Empty
                                        : xElementErrorNode.Attributes["message"].Value;
                    var source = xElementErrorNode.Attributes["source"] == null
                                        ? string.Empty
                                        : xElementErrorNode.Attributes["source"].Value;
                    var detail = xElementErrorNode.Attributes["detail"] == null
                                        ? string.Empty
                                        : xElementErrorNode.Attributes["detail"].Value;

                    var splitString = detail.Split(new[] { " at " }, StringSplitOptions.None);

                    #region Error Area
                    var invalidErrorAreas = new[] { "System.", "CuttingEdge." };
                    var errorArea = "";
                    //foreach (var counter in splitString.Reverse())
                    foreach (var counter in splitString)
                    {
                        if (invalidErrorAreas.Any(counter.StartsWith))
                        {
                            continue;
                        }
                        var index = counter.IndexOf("(", StringComparison.Ordinal);
                        if (index > 0)
                        {
                            errorArea = counter.Substring(0, index);
                            break;
                        }
                        errorArea = counter;
                        break;
                    }
                    #endregion

                    var errorTime = xElementErrorNode.Attributes["time"] == null
                                        ? string.Empty
                                        : xElementErrorNode.Attributes["time"].Value;

                    var pathTranslated = string.Empty;
                    if (xElementPathTranslatedNode != null)
                    {
                        var htmlAttribute = xElementPathTranslatedNode.SelectSingleNode("value").Attributes["string"];
                        pathTranslated = htmlAttribute == null ? string.Empty : htmlAttribute.Value;
                    }

                    var httpReferer = string.Empty;
                    if (xElementHttpRefererNode != null)
                    {
                        var htmlAttribute = xElementHttpRefererNode.SelectSingleNode("value").Attributes["string"];
                        httpReferer = htmlAttribute == null ? string.Empty : htmlAttribute.Value;
                    }

                    var sessionId = string.Empty;
                    if (xElementHTTPASPFILTERSESSIONIDNode != null)
                    {
                        var htmlAttribute =
                            xElementHTTPASPFILTERSESSIONIDNode.SelectSingleNode("value").Attributes["string"];
                        sessionId = htmlAttribute == null ? string.Empty : htmlAttribute.Value;
                    }

                    var queryString = string.Empty;
                    if (xElementQueryString != null)
                    {
                        var htmlAttribute = xElementQueryString.SelectSingleNode("value").Attributes["string"];
                        queryString = htmlAttribute == null ? string.Empty : htmlAttribute.Value;
                    }

                    var httpClientSrcIP = string.Empty;
                    if (xElementHTTPClientSrcIP != null)
                    {
                        var htmlAttribute = xElementHTTPClientSrcIP.SelectSingleNode("value").Attributes["string"];
                        httpClientSrcIP = htmlAttribute == null ? string.Empty : htmlAttribute.Value;
                    }

                    var httpSoapAction = string.Empty;
                    if (xElementHttpSoapActionNode != null)
                    {
                        var htmlAttribute = xElementHttpSoapActionNode.SelectSingleNode("value").Attributes["string"];
                        httpSoapAction = htmlAttribute == null ? string.Empty : htmlAttribute.Value;
                    }

                    var sessionStartQueryString = string.Empty;
                    if (xElementQueryNode != null)
                    {
                        var htmlAttribute = xElementQueryNode.SelectSingleNode("value").Attributes["string"];
                        sessionStartQueryString = htmlAttribute == null ? string.Empty : htmlAttribute.Value;
                    }

                    var siteId = string.Empty;
                    if (xElementSiteId != null)
                    {
                        var htmlAttribute = xElementSiteId.SelectSingleNode("value").Attributes["string"];
                        siteId = htmlAttribute == null ? string.Empty : htmlAttribute.Value;
                    }


                    var userName = string.Empty;
                    if (xElementEOLUserName != null)
                    {
                        var htmlAttribute = xElementEOLUserName.SelectSingleNode("value").Attributes["string"];
                        userName = htmlAttribute == null ? string.Empty : htmlAttribute.Value;
                    }

                    var model = string.Empty;
                    if (xElementModel != null)
                    {
                        var htmlAttribute = xElementModel.SelectSingleNode("value").Attributes["string"];
                        model = htmlAttribute == null ? string.Empty : htmlAttribute.Value;
                    }

                    string firstName = string.Empty;
                    string lastName = string.Empty;

                    if (xElementStateOfPerson != null)
                    {
                        var htmlAttribute = xElementStateOfPerson.SelectSingleNode("value").Attributes["string"];
                        var stateOfPersonValue = htmlAttribute == null ? string.Empty : htmlAttribute.Value;
                        firstName = GetBetween(stateOfPersonValue, "&lt;FirstName&gt;", "&lt;/FirstName&gt;");
                        lastName = GetBetween(stateOfPersonValue, "&lt;LastName&gt;", "&lt;/LastName&gt;");
                        userName = GetBetween(stateOfPersonValue, "&lt;UserName&gt;", "&lt;/UserName&gt;");
                    }

                    var error1 = new error
                    {
                        errorType = errorType,
                        //host = host,
                        message = message,
                        //source = source,
                        //errorArea = errorArea,
                        //queryString = queryString,
                        //siteId = siteId,
                        //model = model,
                        //eolUserName = userName,
                        //name = string.Format("{0} {1}", firstName, lastName),
                        //httpClientSrcIP = httpClientSrcIP,
                        errorTime = DateTime.Parse(errorTime).ToString("yyyy-MM-ddThh:mm:ss"),
                        //sessionId = sessionId,
                        //pathTranslated = pathTranslated,
                        //httpSoapAction = httpSoapAction,
                        //sessionStartQueryString = sessionStartQueryString,
                        //httpReferer = httpReferer,
                        filePath = file
                    };
                    #endregion

                    errors.Add(error1);
                //}
                });
            }
            catch (Exception exception)
            {
                LogManager.GetCurrentClassLogger().Fatal(exception.ToString());
            }
            return errors;
        }

        public static string GetBetween(string value, string x, string y)
        {
            var num1 = value.IndexOf(x, System.StringComparison.Ordinal);
            var num2 = value.IndexOf(y, System.StringComparison.Ordinal);
            if (num1 == -1 || num1 == -1)
                return string.Empty;
            var startIndex = num1 + x.Length;
            if (startIndex < num2)
                return value.Substring(startIndex, num2 - startIndex).Trim();
            return string.Empty;
        }
    }
}
