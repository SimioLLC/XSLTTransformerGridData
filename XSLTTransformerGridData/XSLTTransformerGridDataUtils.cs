using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SimioAPI;
using SimioAPI.Extensions;

namespace XSLTransformerGridData
{
    public static class XSLTTransformerGridDataUtils
    {
        /// <summary>
        /// Examines the given DataSet to compute column information.
        /// </summary>
        internal static DataTable ConvertExportContextToDataTable(IGridDataOpenImportLocalRecordsContext importLocalRecordsContext, string tableName)
        {
            // New table
            var dataTable = new DataTable();
            dataTable.TableName = tableName;
            dataTable.Locale = CultureInfo.InvariantCulture;

            List<IGridDataExportColumnInfo> colImportLocalRecordsColumnInfoList = new List<IGridDataExportColumnInfo>();

            foreach (var col in importLocalRecordsContext.Records.Columns)
            {
                colImportLocalRecordsColumnInfoList.Add(col);
                var dtCol = dataTable.Columns.Add(col.Name, Nullable.GetUnderlyingType(col.Type) ?? col.Type);
            }

            // Add Rows to data table
            foreach (var record in importLocalRecordsContext.Records)
            {
                object[] thisRow = new object[dataTable.Columns.Count];

                int dbColIndex = 0;
                foreach (var colExportLocalRecordsColumnInfo in colImportLocalRecordsColumnInfoList)
                {
                    var valueObj = record.GetNativeObject(dbColIndex);
                    thisRow[dbColIndex] = valueObj;
                    dbColIndex++;
                }

                dataTable.Rows.Add(thisRow);
            }

            return dataTable;
        }
    }
}


