using System;
using System.IO;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Xsl;
using Saxon.Api;

namespace Simio.Xml
{
    public static class XsltTransform
    {
        public class TransformResult
        {
            public DataSet DataSet { get; set; }
            public string XmlTransformError { get; set; }
            public string DataSetLoadError { get; set; }
        }

        public static string TransformXml(string xmlData, string xslData)
        {
            var xsltProcessor = new Processor();
            var documentBuilder = xsltProcessor.NewDocumentBuilder();
            documentBuilder.BaseUri = new Uri("file://");
            var xdmNode = documentBuilder.Build(new StringReader(xmlData));

            var xsltCompiler = xsltProcessor.NewXsltCompiler();
            var xsltExecutable = xsltCompiler.Compile(new StringReader(xslData));
            var xsltTransformer = xsltExecutable.Load();
            xsltTransformer.InitialContextNode = xdmNode;

            var results = new XdmDestination();

            xsltTransformer.Run(results);
            return results.XdmNode.OuterXml;
        }


        public static TransformResult TransformXmlToDataSet(string rawXml, string stylesheet, Action<string> setFinalXmlResult)
        {          

            // If the XSLT loaded and transformed the XML correctly, now try loading that into a dataset
            try
            {
                var xmlData = TransformXml(rawXml, stylesheet);

                var dataSet = new DataSet();

                dataSet.ReadXml(new XmlTextReader(new StringReader(xmlData)));
 
                return new TransformResult()
                {
                    DataSet = dataSet
                };
            }
            catch (Exception e)
            {
                var errorString = $"Error processing XML for DataSet Table: {e.Message}";
                while (e.InnerException != null)
                {
                    errorString += System.Environment.NewLine + e.InnerException.Message;
                    e = e.InnerException;
                }

                return new TransformResult()
                {
                    DataSetLoadError = errorString
                };
            }
        }
    }
}
