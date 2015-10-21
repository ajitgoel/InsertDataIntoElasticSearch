namespace InsertDataIntoElasticSearch
{
    using System;

    using Nest;

    //[ElasticType(IdProperty = "filePath")]
    public class error
    {
        public string errorType { get; set; }
        public string message { get; set; }
        //public string source { get; set; }
        //public string errorArea { get; set; }

        //public string queryString { get; set; }
        //public string siteId { get; set; }
        //public string model { get; set; }
        //public string modelState { get; set; }
        //public string eolUserName { get; set; }
        //public string name { get; set; }

        //public string httpClientSrcIP { get; set; }
        public string errorTime { get; set; }
        //public string sessionId { get; set; }
        //public string rpiSession { get; set; }
        //public string isRPISessionIdEquaulToASPNetSessionId { get; set; }
        //public string aspxAUTHCookie { get; set; }

        //public string httpSoapAction { get; set; }
        //public string pathTranslated { get; set; }
        //public string httpReferer { get; set; }

        //public string sessionStartQueryString { get; set; }

        public string filePath { get; set; }
        //public string host { get; set; }
        //public string returnUrl { get; set; }
    }
}