using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Reflection;

namespace WFProcessImport.Lib
{
    public static class DataTableHelper
    {
        public static List<T> DataTableMapTo<T>(this DataTable dataTable) where T : class, new()
        {
            if (dataTable == null) { return null; }

            var map = new List<Tuple<DataColumn, PropertyInfo>>();
            foreach (PropertyInfo pi in typeof(T).GetProperties())
            {
                if (dataTable.Columns.Contains(pi.Name))
                {
                    map.Add(new Tuple<DataColumn, PropertyInfo>(dataTable.Columns[pi.Name], pi));
                }
            }

            var list = new List<T>(dataTable.Rows.Count);
            foreach (DataRow row in dataTable.Rows)
            {
                if (row == null) { continue; }

                var item = new T();
                foreach (Tuple<DataColumn, PropertyInfo> pair in map)
                {
                    object value = row[pair.Item1];
                    if (value is DBNull) { value = null; }

                    pair.Item2.SetValue(item, value, null);
                }
                list.Add(item);
            }
            return list;
        }

        public static DateTime ConvertStrToDate(string srcValue, string dateFormat)
        {
          // state="sent" last_saved_date="20.08.2007 16:22:15"

            if (!string.IsNullOrEmpty(srcValue)
                && DateTime.TryParseExact(srcValue, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateResult))
            {
                return dateResult;
            }
            return DateTime.MinValue;
        }
    }
}
