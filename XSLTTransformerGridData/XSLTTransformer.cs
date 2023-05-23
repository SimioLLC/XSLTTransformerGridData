using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using SimioAPI;
using SimioAPI.Extensions;
using System.IO;
using System.Xml;
using XSLTransformerGridData;

namespace XSLTTransformerGridData
{
    public class XSLTTranformerImporterDefinition : IGridDataImporterDefinition
    {
        public string Name => "XSLT Transformer";
        public string Description => "XSLT Transformer";
        public Image Icon => null;

        static readonly Guid MY_ID = new Guid("9113ce8a-3245-4f5c-aaf3-ba6091d7180b");
        public Guid UniqueID => MY_ID;

        public IGridDataImporter CreateInstance(IGridDataImporterContext context)
        {
            return new XSLTTransformer(context);
        }

        public void DefineSchema(IGridDataSchema schema)
        {
            var sourceTablesProp = schema.PerTableProperties.AddNameValuePairsProperty("SourceTables", null);
            sourceTablesProp.DisplayName = "SourceTables";
            sourceTablesProp.Description = "SourceTables.";
            sourceTablesProp.DefaultValue = String.Empty;

            var nestedRelationshipsProp = schema.PerTableProperties.AddNameValuePairsProperty("NestedRelationships", null);
            nestedRelationshipsProp.DisplayName = "Nested Relationships";
            nestedRelationshipsProp.Description = "Nested Relationships.";
            nestedRelationshipsProp.DefaultValue = String.Empty;

            var stylesheetProp = schema.PerTableProperties.AddXSLTProperty("Stylesheet");
            stylesheetProp.Description = "The transform to apply to the data source data to generate the destination data.";
            stylesheetProp.DefaultValue =
@"<xsl:stylesheet version=""1.0"" xmlns:xsl=""http://www.w3.org/1999/XSL/Transform"">
    <xsl:template match=""node()|@*"">
      <xsl:copy>
        <xsl:apply-templates select=""node()|@*""/>
      </xsl:copy>
    </xsl:template>
</xsl:stylesheet>";
            stylesheetProp.GetXML += StylesheetProp_GetXML;

            var sourceXMLFileNameProp = schema.PerTableProperties.AddFileProperty("SourceXMLFileName");
            sourceXMLFileNameProp.DisplayName = "Source XML File Name";
            sourceXMLFileNameProp.Description = "Source XML File Name.";
            sourceXMLFileNameProp.DefaultValue = String.Empty;

            var sourceXSDFileNameProp = schema.PerTableProperties.AddFileProperty("SourceXSDFileName");
            sourceXSDFileNameProp.DisplayName = "Source XSD File Name";
            sourceXSDFileNameProp.Description = "Source XSD File Name.";
            sourceXSDFileNameProp.DefaultValue = String.Empty;
        }

        private void StylesheetProp_GetXML(object sender, XSLTAddInPropertyGetXMLEventArgs e)
        {
            //This is called when the stylesheet editor pops up.We want to provide the XML data we would expect to come back during an actual import,
            var sourceTablesStr = (string)e.OtherProperties?["SourceTables"]?.Value;
            var sourceTables = AddInPropertyValueHelper.NameValuePairsFromString(sourceTablesStr);
            var sourceTablesArr = sourceTables.Select(z => z.Value).ToArray();
            if (sourceTablesArr.Length == 0)
            {
                e.XML = "No Soure Data Found";
            }
            else
            {
                // Where Tranformation Happens
                //DataSet destinationTableDataSet = new DataSet();
                //foreach (var sourceTableStr in sourceTablesArr)
                //{
                //    var sourceTable = e.GetLocalTableRecords(sourceTableStr);
                //    var soruceDataTable = XSLTTransformerGridDataUtils.ConvertExportContextToDataTable(sourceTable, sourceTableStr);
                //    destinationTableDataSet.Tables.Add(soruceDataTable);
                //}

                //e.XML = destinationTableDataSet.GetXml();

                e.XML = "Getting Source Data Is Not Supported Until e.GetLocalTableRecords() can be added to XSLTAddInPropertyGetXMLEventArgs";
            }
        }
    }

    class XSLTTransformer : IGridDataImporter
    {
        public XSLTTransformer(IGridDataImporterContext context)
        {
        }

        public OpenImportDataResult OpenData(IGridDataOpenImportDataContext openContext)
        {
            var sourceTablesStr = (string)openContext.Settings.GridDataSettings[openContext.TableName]?.Properties["SourceTables"]?.Value;
            var sourceTables = AddInPropertyValueHelper.NameValuePairsFromString(sourceTablesStr);
            var sourceTablesArr = sourceTables.Select(z => z.Value).ToArray();
            if (sourceTablesArr.Length == 0)
                return OpenImportDataResult.Failed("Source Tables have been defined.");

            var nestedRelationshipsStr = (string)openContext.Settings.GridDataSettings[openContext.TableName]?.Properties["NestedRelationships"]?.Value;
            var nestedRelationships = AddInPropertyValueHelper.NameValuePairsFromString(nestedRelationshipsStr);
            var nestedRelationshipsArr = nestedRelationships.Select(z => z.Value).ToArray();

            var stylesheet = (string)openContext.Settings.GridDataSettings[openContext.TableName]?.Properties["Stylesheet"]?.Value;
            if (String.IsNullOrWhiteSpace(stylesheet))
            {
                return OpenImportDataResult.Failed("Stylesheet table parameter is not specified.");
            }

            var sourceXMLFileName = (string)openContext.Settings.GridDataSettings[openContext.TableName]?.Properties?["SourceXMLFileName"]?.Value;
            var sourceXSDFileName = (string)openContext.Settings.GridDataSettings[openContext.TableName]?.Properties?["SourceXSDFileName"]?.Value;

            // Where Tranformation Happens
            DataSet sourceTablesDataSet = new DataSet();
            foreach (var sourceTableStr in sourceTablesArr)
            {
                var sourceTable = openContext.GetLocalTableRecords(sourceTableStr);
                var soruceDataTable = XSLTTransformerGridDataUtils.ConvertExportContextToDataTable(sourceTable, sourceTableStr);
                sourceTablesDataSet.Tables.Add(soruceDataTable);
            }

            foreach (var nestedRelationshipStr in nestedRelationshipsArr)
            {
                var tableAndColumnArr = nestedRelationshipStr.Split('.');
                var relation = sourceTablesDataSet.Relations.Add(sourceTablesDataSet.Tables[tableAndColumnArr[0]].Columns[tableAndColumnArr[1]], sourceTablesDataSet.Tables[tableAndColumnArr[2]].Columns[tableAndColumnArr[3]]);
                relation.Nested = true;
            }

            if (sourceXMLFileName.Length > 0) sourceTablesDataSet.WriteXml(sourceXMLFileName);
            if (sourceXSDFileName.Length > 0) sourceTablesDataSet.WriteXmlSchema(sourceXSDFileName);

            DataSet destinationTableDataSet = new DataSet();

            var transformedResult = Simio.Xml.XsltTransform.TransformXmlToDataSet(sourceTablesDataSet.GetXml(), stylesheet, null);
            if (transformedResult.XmlTransformError != null)
                return new OpenImportDataResult() { Result = GridDataOperationResult.Failed, Message = transformedResult.XmlTransformError };
            if (transformedResult.DataSetLoadError != null)
                return new OpenImportDataResult() { Result = GridDataOperationResult.Failed, Message = transformedResult.DataSetLoadError };
            if (transformedResult.DataSet.Tables[0].Rows.Count > 0)
            {
                transformedResult.DataSet.AcceptChanges();
                destinationTableDataSet.Merge(transformedResult.DataSet);
                destinationTableDataSet.AcceptChanges();
            }

            return new OpenImportDataResult()
            {
                Result = GridDataOperationResult.Succeeded,
                Records = new XSLTTransformerGridDataRecords(destinationTableDataSet)
            };
        }

        public string GetDataSummary(IGridDataSummaryContext context)
        {
            if (context == null)
                return null;

            var sourceTablesStr = (string)context.Settings.GridDataSettings[context.GridDataName]?.Properties["SourceTables"]?.Value;
            var sourceTables = AddInPropertyValueHelper.NameValuePairsFromString(sourceTablesStr);
            var sourceTablesArr = sourceTables.Select(z => z.Value).ToArray();

            if (sourceTablesArr.Length == 0)
                return null;

            var sourceTablesJoin = String.Join(",", sourceTablesArr);

            return String.Format("Bound to {0} ", sourceTablesJoin);
        }

        public void Dispose()
        {
        }
    }

    class XSLTTransformerGridDataRecords : IGridDataRecords
    {
        // Acts as a cached dataset, specifically for cases of stored procedures, which may have side effects, so we only
        // want to 'call' once, not once to get schema, then AGAIN to get the data. We'll also use this for SQL 
        // statements... that really could be anything.
        DataSet _dataSet;

        public XSLTTransformerGridDataRecords(DataSet dataSet)
        {
            _dataSet = dataSet;
        }

        #region IGridDataRecords Members

        List<GridDataColumnInfo> _columnInfo;
        List<GridDataColumnInfo> ColumnInfo
        {
            get
            {
                if (_columnInfo == null)
                {
                    _columnInfo = new List<GridDataColumnInfo>();

                    if (_dataSet.Tables.Count > 0)
                    {
                        foreach (DataColumn dc in _dataSet.Tables[0].Columns)
                        {
                            var name = dc.ColumnName;
                            var type = dc.DataType;

                            _columnInfo.Add(new GridDataColumnInfo()
                            {
                                Name = name,
                                Type = type
                            });
                        }
                    }
                }

                return _columnInfo;
            }
        }

        public IEnumerable<GridDataColumnInfo> Columns
        {
            get { return ColumnInfo; }
        }

        #endregion

        #region IEnumerable<IGridDataRecord> Members

        public IEnumerator<IGridDataRecord> GetEnumerator()
        {
            if (_dataSet.Tables.Count > 0)
            {
                foreach (DataRow dr in _dataSet.Tables[0].Rows)
                {
                    yield return new XSLTTransformerGridDataRecord(dr);
                }

            }
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
        }

        #endregion
    }

    class XSLTTransformerGridDataRecord : IGridDataRecord
    {
        private readonly DataRow _dr;
        public XSLTTransformerGridDataRecord(DataRow dr)
        {
            _dr = dr;
        }

        #region IGridDataRecord Members

        public string this[int index]
        {
            get
            {
                var theValue = _dr[index];

                // Simio will first try to parse dates in the current culture
                if (theValue is DateTime)
                    return ((DateTime)theValue).ToString();

                return String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}", _dr[index]);
            }
        }

        #endregion
    }
}

