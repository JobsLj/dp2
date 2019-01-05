using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Diagnostics;

using DigitalPlatform.Text;

namespace DigitalPlatform.dp2.Statis
{
    // ��
    public class Line : IComparable
    {
        internal object[] cells = null;

        internal string strKey = null;

        public Line(int nColumnsHint)
        {
            if (nColumnsHint > 0)
                cells = new object[nColumnsHint];
        }

        public bool IsNull(int nIndex)
        {
            if (cells == null)
                return true;
            if (nIndex >= cells.Length)
                return true;

            // 2007/5/18
            if (cells[nIndex] == null)
                return true;

            return false;
        }

        // �õ�һ����Ԫֵ���Զ�ת��Ϊ�ַ���
        // ���һ����Ԫ�������ù����ݣ��򷵻�strDefaultValue
        // parameters:
        //      nIndex  �����������Ϊ-1����ʾϣ����ȡEntry��
        public string GetString(int nIndex,
            string strDefaultValue)
        {
            // 2007/10/26
            if (nIndex == -1)
                return this.Entry;

            if (cells == null)
                return strDefaultValue;
            if (nIndex >= cells.Length)
                return strDefaultValue;

            Object obj = cells[nIndex];
            if (obj != null)
            {
                if (obj is Int32)
                    return Convert.ToString((Int32)obj);
                if (obj is Int64)
                    return Convert.ToString((Int64)obj);
                if (obj is double)
                    return Convert.ToString((double)obj);
                if (obj is decimal)
                    return Convert.ToString((decimal)obj);

                if (obj is string)
                {
                    string strText = (string)obj;

                    // 2008/4/3
                    if (String.IsNullOrEmpty(strText) == true)
                        return strDefaultValue;
                    return strText;
                }
                throw (new Exception("��֧�ֵ��������� " + obj.GetType().ToString()));
            }

            return strDefaultValue;

        }

        // �õ�һ����Ԫֵ���Զ�ת��Ϊ�ַ���
        // ���һ����Ԫ�������ù����ݣ��򷵻�""
        // parameters:
        //      nIndex  �����������Ϊ-1����ʾϣ����ȡEntry��
        public string GetString(int nIndex)
        {
            /*
             if (cells == null)
                 return "";
             if (nIndex >= cells.Length)
                 return "";

             Object obj = cells[nIndex];
             if (obj != null) 
             {
                 if ((obj is Int32) || (obj is Int64))
                     return Convert.ToString((Int64)obj);
                 if (obj is string)
                     return (string)obj;
                 throw(new Exception("��֧�ֵ���������"));
             }

             return "";
             */
            return GetString(nIndex, "");
        }

        // �õ�100���������ֵ���ַ���
        // Exception:
        //		Exception
        //		???
        public string GetPriceString(int nIndex)
        {
            Int64 v = GetInt64(nIndex);

            return StatisUtil.Int64ToPrice(v);
        }

        // �õ�һ����Ԫֵ���Զ�ת��ΪInt64����
        // ���һ����Ԫ�������ù����ݣ��򷵻�0
        public Int64 GetInt64(int nIndex)
        {
            if (cells == null)
                return (Int64)0;
            if (nIndex >= cells.Length)
                return (Int64)0;

            Object obj = cells[nIndex];
            if (obj != null)
            {
                if ((obj is Int32))
                    return (Int32)obj;
                if ((obj is Int64))
                    return (Int64)obj;
                if (obj is decimal)
                    return Convert.ToInt64((decimal)obj);

                if (obj is string)
                    return Convert.ToInt64((string)obj);

                throw (new Exception("��֧�ֵ��������� " + obj.GetType().ToString()));
            }

            return (Int64)0;
        }

        // �õ�һ����Ԫֵ���Զ�ת��Ϊdouble����
        // ���һ����Ԫ�������ù����ݣ��򷵻�0
        public double GetDouble(int nIndex)
        {
            if (cells == null)
                return (double)0;
            if (nIndex >= cells.Length)
                return (double)0;

            Object obj = cells[nIndex];
            if (obj != null)
            {
                if ((obj is Int32) || (obj is Int64))
                    return Convert.ToDouble(obj);   // ע�⣬��(double)ֱ��ת���ǲ��е�
                else if (obj is double)
                    return (double)obj;
                else if (obj is decimal)
                    return Convert.ToDouble((decimal)obj);
                else if (obj is string)
                {
                    string strText = (string)obj;
                    try
                    {
                        return Convert.ToDouble(strText);   // BUG!!! ����return
                    }
                    catch (Exception ex)
                    {
                        // 2008/4/24
                        throw new Exception("�ַ���ֵ '" + strText + "' ��ת��Ϊdouble����ʱ��������: " + ex.Message);
                    }
                }
                else
                    throw (new Exception("��֧�ֵ��������� " + obj.GetType().ToString()));
            }

            return (double)0;
        }

        public object GetObject(int nIndex)
        {
            if (cells == null)
                return null;
            if (nIndex >= cells.Length)
                return null;

            return cells[nIndex];
        }

        // 2014/6/8
        public object[] GetAllCells()
        {
            object[] result = new object[cells.Length + 1];
            result[0] = this.strKey;
            Array.Copy(cells, 0, result, 1, cells.Length);
            return result;
        }

        // 2008/11/29
        // �õ�һ����Ԫֵ���Զ�ת��Ϊdecimal����
        // ���һ����Ԫ�������ù����ݣ��򷵻�0
        public decimal GetDecimal(int nIndex)
        {
            if (cells == null)
                return (decimal)0;
            if (nIndex >= cells.Length)
                return (decimal)0;

            Object obj = cells[nIndex];
            if (obj != null)
            {
                if ((obj is Int32) || (obj is Int64))
                    return Convert.ToDecimal(obj);   // ע�⣬��(decimal)ֱ��ת���ǲ��е�
                else if (obj is double)
                    return Convert.ToDecimal((double)obj);
                else if (obj is decimal)
                    return (decimal)obj;
                else if (obj is string)
                {
                    string strText = (string)obj;
                    try
                    {
                        return Convert.ToDecimal(strText);
                    }
                    catch (Exception ex)
                    {
                        // 2008/4/24
                        throw new Exception("�ַ���ֵ '" + strText + "' ��ת��Ϊdecimal����ʱ��������: " + ex.Message);
                    }
                }
                else
                    throw (new Exception("��֧�ֵ��������� " + obj.GetType().ToString()));
            }

            return (decimal)0;
        }

        // �õ�һ����Ԫ��Object����ֵ��
        // ���һ����Ԫ�������ù����ݣ��򷵻�null
        // parameters:
        //      nIndex  �����������Ϊ-1����ʾϣ����ȡEntry��
        public object this[int nIndex]
        {
            get
            {
                // 2007/10/26
                if (nIndex == -1)
                    return this.Entry;

                if (cells == null)
                    return null;
                if (nIndex >= cells.Length)
                    return null;
                return cells[nIndex];
            }
        }

        public int Count
        {
            get
            {
                if (cells == null)
                    return 0;
                return cells.Length;
            }
        }

        public int CompareTo(object obj)
        {
            // �൱��this - obj��Ч��

            if (obj is Line)
            {
                Line line = (Line)obj;

                return String.Compare(this.strKey, line.strKey, true);
            }

            throw new ArgumentException("object is not a Line");
        }

        // �б����ַ���
        public string Entry
        {
            get
            {
                return strKey;
            }
            set
            {
                // TODO: ����hashtable��item key?
                strKey = value;
            }
        }

        // ȷ���пռ��㹻
        void EnsureCells(int nColumn)
        {
            if (cells == null)
            {
                cells = new object[nColumn + 1];
            }
            else if (cells.Length <= nColumn)
            {
                object[] temp = new object[nColumn + 1];
                // ����
                Array.Copy(cells, 0, temp, 0, cells.Length);
                cells = temp;
            }
        }

        // Ϊһ������һ��ֵ
        public void SetValue(int nColumn,
            object value)
        {
            EnsureCells(nColumn);

            cells[nColumn] = value;
        }

        // Ϊһ�е�����ֵ����
        // ������ֻ��Ӧ����Int32��Int64ֵ���͵����ϣ�������׳��쳣
        // parameters:
        //		createValue	����е�Ԫ�����ڣ�����ô�ֵ��ʼ����
        //		incValue	����е�Ԫ�Ѿ����ڣ�����ô�ֵ����ԭ����ֵ���޸Ļ�
        public void IncValue(
            int nColumn,
            Int64 createValue,
            Int64 incValue)
        {
            EnsureCells(nColumn);

            if (cells[nColumn] == null)
            {
                cells[nColumn] = createValue;
            }
            else
            {
                object oldvalue = cells[nColumn];
                if (oldvalue is Int32)
                {
                    Int64 v = (Int32)oldvalue;
                    v += incValue;
                    cells[nColumn] = v;
                }
                else if (oldvalue is Int64)
                {
                    Int64 v = (Int64)oldvalue;
                    v += incValue;
                    cells[nColumn] = v;
                }
                else
                {
                    throw (new Exception("��" + Convert.ToString(nColumn) + "���ͱ���ΪInt32��Int64"));
                }
            }
        }

        // Ϊһ�е�����ֵ����
        // ������ֻ��Ӧ����Int32��Int64ֵ���͵����ϣ�������׳��쳣
        // parameters:
        //		createValue	����е�Ԫ�����ڣ�����ô�ֵ��ʼ����
        //		incValue	����е�Ԫ�Ѿ����ڣ�����ô�ֵ����ԭ����ֵ���޸Ļ�
        public void IncValue(
            int nColumn,
            double createValue,
            double incValue)
        {
            EnsureCells(nColumn);

            if (cells[nColumn] == null)
            {
                cells[nColumn] = createValue;
            }
            else
            {
                object oldvalue = cells[nColumn];
                if ((oldvalue is Int64)
                    || (oldvalue is Int32))
                {
                    double v = Convert.ToDouble(oldvalue);
                    v += incValue;
                    cells[nColumn] = v;
                }
                else if (oldvalue is double)
                {
                    double v = (double)oldvalue;
                    v += incValue;
                    cells[nColumn] = v;
                }
                else if (oldvalue is decimal)
                {
                    double v = Convert.ToDouble((decimal)oldvalue);
                    v += incValue;
                    cells[nColumn] = v;
                }
                else
                {
                    throw (new Exception("��" + Convert.ToString(nColumn) + "���ͱ���ΪInt32��Int64��double"));
                }
            }
        }

        // 2008/11/29
        // Ϊһ�е�����ֵ����
        // ������ֻ��Ӧ����Int32��Int64ֵ���͵����ϣ�������׳��쳣
        // parameters:
        //		createValue	����е�Ԫ�����ڣ�����ô�ֵ��ʼ����
        //		incValue	����е�Ԫ�Ѿ����ڣ�����ô�ֵ����ԭ����ֵ���޸Ļ�
        public void IncValue(
            int nColumn,
            decimal createValue,
            decimal incValue)
        {
            EnsureCells(nColumn);

            if (cells[nColumn] == null)
            {
                cells[nColumn] = createValue;
            }
            else
            {
                object oldvalue = cells[nColumn];
                if ((oldvalue is Int64)
                    || (oldvalue is Int32))
                {
                    decimal v = Convert.ToDecimal(oldvalue);
                    v += incValue;
                    cells[nColumn] = v;
                }
                else if (oldvalue is double)
                {
                    decimal v = Convert.ToDecimal((double)oldvalue);
                    v += incValue;
                    cells[nColumn] = v;
                }
                else if (oldvalue is decimal)
                {
                    decimal v = (decimal)oldvalue;
                    v += incValue;
                    cells[nColumn] = v;
                }
                else
                {
                    throw (new Exception("��" + Convert.ToString(nColumn) + "���ͱ���ΪInt32��Int64��decimal"));
                }
            }
        }

        // Ϊһ�е��ַ���ֵ����
        // ������ֻ��Ӧ����stringֵ���͵����ϣ�������׳��쳣
        // parameters:
        //		createValue	����е�Ԫ�����ڣ�����ô�ֵ��ʼ����
        //		incValue	����е�Ԫ�Ѿ����ڣ������ԭ����ֵ����׷�Ӵ�ֵ���޸Ļ�
        public void IncValue(
            int nColumn,
            string createValue,
            string incValue)
        {
            EnsureCells(nColumn);

            if (cells[nColumn] == null)
            {
                cells[nColumn] = createValue;
            }
            else
            {
                object oldvalue = cells[nColumn];
                if (oldvalue is string)
                {
                    string v = (string)oldvalue;
                    v += incValue;
                    cells[nColumn] = v;
                }
                else
                {
                    throw (new Exception("��" + Convert.ToString(nColumn) + "���ͱ���Ϊstring"));
                }
            }
        }

        // Ϊһ�е��ַ���ֵ����
        // ������ֻ��Ӧ����stringֵ���͵����ϣ�������׳��쳣
        // parameters:
        //		createValue	����е�Ԫ�����ڣ�����ô�ֵ��ʼ����
        //		incValue	����е�Ԫ�Ѿ����ڣ������ԭ����ֵ����׷�Ӵ�ֵ���޸Ļ�
        public void IncCurrency(
            int nColumn,
            string createValue,
            string incValue)
        {
            EnsureCells(nColumn);

            if (cells[nColumn] == null)
            {
                cells[nColumn] = createValue;
            }
            else
            {
                object oldvalue = cells[nColumn];
                if (oldvalue is string)
                {
                    string v = (string)oldvalue;

                    // ���������۸��ַ���
                    v = PriceUtil.JoinPriceString(v,
                        incValue);

                    string strSumPrices = "";
                    string strError = "";
                    // ������"-123.4+10.55-20.3"�ļ۸��ַ����鲢����
                    int nRet = PriceUtil.SumPrices(v,
            out strSumPrices,
            out strError);
                    if (nRet == 0)
                        v = strSumPrices;
                    if (nRet == -1)
                        throw new Exception("���ܽ���ַ��� '" + v + "' ʱ����" + strError);

                    // v += incValue;
                    cells[nColumn] = v;
                }
                else
                {
                    throw (new Exception("��" + Convert.ToString(nColumn) + "���ͱ���Ϊstring"));
                }
            }
        }

    }

    /// <summary>
    /// ��������ͳ�Ƶ�2ά�ڴ���
    /// </summary>
    public class Table : IEnumerable
    {
        Hashtable lines = new Hashtable();

        List<Line> sorted = null;

        int m_nColumnsHint = 0;	// ��ʾ�������

        public int HintColumns
        {
            get
            {
                return this.m_nColumnsHint;
            }
        }

        public int GetMaxColumnCount()
        {
            int nResult = 0;
            foreach (string key in this.lines.Keys)
            {
                Line line = (Line)this.lines[key];
                if (line.Count > nResult)
                    nResult = line.Count;
            }

            return nResult;
        }

        public Table(int nColumnsHint)
        {
            m_nColumnsHint = nColumnsHint;
        }

        // 2013/6/14
        public ICollection Keys
        {
            get
            {
                return this.lines.Keys;
            }
        }

        // 2016/5/19
        public IEnumerator GetEnumerator()
        {
            if (this.sorted != null)
            {
                for(int i = 0;i<this.Count;i++)
                {
                    Line line = this[i];
                    yield return line;
                }
            }
            else
            {
                foreach(string key in this.lines.Keys)
                {
                    Line line = this[key];
                    yield return line;
                }
            }
        }

        // д��һ����Ԫ��ֵ
        public void SetValue(string strEntry,
            int nColumn,
            object value)
        {
            // ���line�����Ƿ����
            Line line = EnsureLine(strEntry, m_nColumnsHint);

            Debug.Assert(line != null, "line������Ӧ��!=null");

            line.SetValue(nColumn, value);
        }

        public Line SearchLine(string strEntry)
        {
            return (Line)lines[strEntry];
        }

        public int Count
        {
            get
            {
                return lines.Count;
            }
        }

        public object SearchValue(string strEntry,
            int nColumn)
        {
            Line line = (Line)lines[strEntry];
            if (line == null)
                return null;
            if (line.cells == null)
                return null;
            if (line.cells.Length <= nColumn)
                return null;
            return line.cells[nColumn];
        }

        public void RemoveLine(string strEntry)
        {
            lines.Remove(strEntry);
        }

        // ɾ����nStart��ʼ��ĩβ����
        public void RemoveLines(int nStart)
        {
            RemoveLines(nStart, lines.Count);   // ?? lines.Count - nStart
        }

        // ɾ��ָ����Χ����
        public void RemoveLines(int nStart,
            int nCount)
        {
            int i = 0;
            List<string> keys = new List<string>();
            foreach (string strKey in lines.Keys)
            {
                if (i >= nStart && i < nStart + nCount)
                    keys.Add(strKey);

                if (i >= nStart + nCount)
                    break;

                i++;
            }

            foreach (string strKey in keys)
            {
                lines.Remove(strKey);
            }
        }

        // �õ��ж�����������ڣ�����ʱ����һ��
        public Line EnsureLine(string strEntry,
            int nColumnsHint = -1)
        {
            if (strEntry == null)
                throw new ArgumentException("strEntry ����ֵ��ӦΪ null", "strEntry");

            // ���line�����Ƿ����
            Line line = (Line)lines[strEntry];

            if (line == null)
            {
                if (nColumnsHint == -1)
                    nColumnsHint = this.m_nColumnsHint;

                line = new Line(nColumnsHint);
                line.strKey = strEntry;

                lines.Add(strEntry, line);
            }

            Debug.Assert(line != null, "line������Ӧ��!=null");


            return line;
        }

        public Line this[string strEntry]
        {
            get
            {
                return (Line)lines[strEntry];
            }
        }

        // ��������������
        public Line this[int nIndex]
        {
            get
            {
                // TODO: �����ڼ������µ��л���ɾ�������Ժ�sortedӦ�ûָ�null
                if (sorted == null)
                {
                    throw (new Exception("ʹ��[int nIndex]������֮ǰ����������Sort()��������..."));
                }
                return (Line)sorted[nIndex];
            }
        }

        // ��װ��İ汾 2015/4/2
        public void IncValue(string strEntry,
            int nColumn,
            Int64 value)
        {
            IncValue(strEntry,
                nColumn,
                value,
                value);
        }

        // �ۼ�һ����Ԫ��ֵ
        // createValue	���ָ���ĵ�Ԫ�����ڣ����Դ�ֵ�����µ�Ԫ
        // incValue	���ָ���ĵ�Ԫ�Ѿ����ڣ�����ԭֵ�ϵ�����ֵ��
        public void IncValue(string strEntry,
            int nColumn,
            Int64 createValue,
            Int64 incValue)
        {
            // ���line�����Ƿ����
            Line line = EnsureLine(strEntry, m_nColumnsHint);

            Debug.Assert(line != null, "line������Ӧ��!=null");

            line.IncValue(nColumn, createValue, incValue);
        }

        // �ۼ�һ����Ԫ��ֵ
        // createValue	���ָ���ĵ�Ԫ�����ڣ����Դ�ֵ�����µ�Ԫ
        // incValue	���ָ���ĵ�Ԫ�Ѿ����ڣ�����ԭֵ�ϵ�����ֵ��
        public void IncValue(string strEntry,
            int nColumn,
            double createValue,
            double incValue)
        {
            // ���line�����Ƿ����
            Line line = EnsureLine(strEntry, m_nColumnsHint);

            Debug.Assert(line != null, "line������Ӧ��!=null");

            line.IncValue(nColumn, createValue, incValue);
        }

        // 2008/12/1
        // �ۼ�һ����Ԫ��ֵ
        // createValue	���ָ���ĵ�Ԫ�����ڣ����Դ�ֵ�����µ�Ԫ
        // incValue	���ָ���ĵ�Ԫ�Ѿ����ڣ�����ԭֵ�ϵ�����ֵ��
        public void IncValue(string strEntry,
            int nColumn,
            decimal createValue,
            decimal incValue)
        {
            // ���line�����Ƿ����
            Line line = EnsureLine(strEntry, m_nColumnsHint);

            Debug.Assert(line != null, "line������Ӧ��!=null");

            line.IncValue(nColumn, createValue, incValue);
        }


        // �ۼ�һ����Ԫ��ֵ
        // createValue	���ָ���ĵ�Ԫ�����ڣ����Դ�ֵ�����µ�Ԫ
        // incValue	���ָ���ĵ�Ԫ�Ѿ����ڣ�����ԭֵ��׷�Ӵ�ֵ��
        public void IncValue(string strEntry,
            int nColumn,
            string createValue,
            string incValue)
        {
            // ���line�����Ƿ����
            Line line = EnsureLine(strEntry, m_nColumnsHint);

            Debug.Assert(line != null, "line������Ӧ��!=null");

            line.IncValue(nColumn, createValue, incValue);
        }

        public void IncCurrency(string strEntry,
    int nColumn,
    string createValue,
    string incValue)
        {
            // ���line�����Ƿ����
            Line line = EnsureLine(strEntry, m_nColumnsHint);

            Debug.Assert(line != null, "line������Ӧ��!=null");

            line.IncCurrency(nColumn, createValue, incValue);
        }


        // �����б��⣬��������
        public void Sort()
        {
            // ��lines�����ж���ָ�븴�Ƶ�ArrayList��
            if (sorted == null)
            {
                // sorted = new ArrayList();
                sorted = new List<Line>();
            }
            else
                sorted.Clear();

            //sorted.AddRange(lines);
            foreach (DictionaryEntry item in lines)
            {
                sorted.Add((Line)item.Value);
            }

            sorted.Sort();
        }

#if NO
        // 2009/9/30
        // �Զ����������
        public void Sort(IComparer comparer)
        {
            // ��lines�����ж���ָ�븴�Ƶ�ArrayList��
            if (sorted == null)
            {
                // sorted = new ArrayList();
                sorted = new List<Line>();
            }
            else
                sorted.Clear();

            foreach (DictionaryEntry item in lines)
            {
                sorted.Add((Line)item.Value);
            }

            sorted.Sort(comparer);
        }
#endif

        public void Sort(IComparer<Line> comparer)
        {
            // ��lines�����ж���ָ�븴�Ƶ�ArrayList��
            if (sorted == null)
            {
                // sorted = new ArrayList();
                sorted = new List<Line>();
            }
            else
                sorted.Clear();

            foreach (DictionaryEntry item in lines)
            {
                sorted.Add((Line)item.Value);
            }

            sorted.Sort(comparer);
        }

        // ���ո���Ҫ������
        // strColumnList	���ŷָ���к��ַ��������򽫰���������ȼ�����
        public void Sort(string strColumnList)
        {
            // ��lines�����ж���ָ�븴�Ƶ�ArrayList��
            if (sorted == null)
            {
                // sorted = new ArrayList();
                sorted = new List<Line>();
            }
            else
                sorted.Clear();

            //sorted.AddRange(lines);
            foreach (DictionaryEntry item in lines)
            {
                sorted.Add((Line)item.Value);
            }

            sorted.Sort(new ComparerClass(strColumnList));
        }

        // �õ�����ĵ�һ��
        public Line FirstHashLine()
        {
            foreach (DictionaryEntry item in lines)
            {
                return (Line)item.Value;
            }

            return null;
        }

        #region IComparer��ComparerClass����������

        public class ComparerClass : IComparer<Line>
        {
            SortColumnCollection sortstyle = null;

            public ComparerClass(string strColumnList)
            {
                sortstyle = new SortColumnCollection();
                sortstyle.Build(strColumnList);
            }

            // Calls CaseInsensitiveComparer.Compare with the parameters reversed.
            int IComparer<Line>.Compare(Line line1, Line line2)
            {
                /*
                if (!(x is Line))
                    throw new ArgumentException("object x is not a Line");
                if (!(y is Line))
                    throw new ArgumentException("object y is not a Line");


                Line line1 = (Line)x;
                Line line2 = (Line)y;
                */
                if (sortstyle == null)
                    throw (new Exception("sortstyle��δ����"));

                for (int i = 0; i < this.sortstyle.Count; i++)
                {
                    SortColumn column = (SortColumn)this.sortstyle[i];

                    int nRet = 0;

                    // ȡ�б�����бȽ�
                    if (column.nColumnNumber == -1)
                    {
                        nRet = column.CompareString(line1.strKey, line2.strKey);
                        if (nRet != 0)
                            return nRet;

#if NO
                        if (column.dataType == DataType.Auto
                            || column.dataType == DataType.Number)
                        {
                            if (line1.strKey.Length == line2.strKey.Length)
                            {
                                nRet = String.Compare(line1.strKey, line2.strKey, column.bIgnorCase);
                            }
                            else
                            {
                                // �Ҷ���?
                                string s1 = line1.strKey;
                                string s2 = line2.strKey;

                                if (s1.Length < s2.Length)
                                {
                                    s1 = s1.PadLeft(s2.Length, ' ');
                                }
                                else if (s1.Length > s2.Length)
                                {
                                    s2 = s2.PadLeft(s1.Length, ' ');
                                }
                                nRet = String.Compare(s1, s2, column.bIgnorCase);

                            }

                            if (column.bAsc == false)
                                nRet = nRet * (-1);
                            if (nRet != 0)
                                return nRet;

                        }
                        else
                        {
                            nRet = String.Compare(line1.strKey, line2.strKey, column.bIgnorCase);
                            if (column.bAsc == false)
                                nRet = nRet * (-1);
                            if (nRet != 0)
                                return nRet;
                        }

#endif
                    }
                    else
                    {
                        object o1 = null;

                        if (column.nColumnNumber < line1.cells.Length)
                            o1 = line1.cells[column.nColumnNumber];

                        object o2 = null;

                        if (column.nColumnNumber < line2.cells.Length)
                            o2 = line2.cells[column.nColumnNumber];

                        nRet = column.CompareObject(o1, o2);
                        if (nRet != 0)
                            return nRet;
#if NO
                        if (column.dataType == DataType.Auto
                            || column.dataType == DataType.Number)
                        {
                            Int64 n1 = 0;
                            Int64 n2 = 0;
                            string s1 = null;
                            string s2 = null;
                            bool bException = false;

                            if ((o1 is Int32)
                                || (o1 is Int64))
                                n1 = (Int64)o1;
                            else if (o1 is string)
                            {
                                try
                                {
                                    n1 = Convert.ToInt64((string)o1);	// �����׳��쳣
                                }
                                catch
                                {
                                    s1 = (string)o1;
                                    bException = true;
                                }
                            }


                            if ((o2 is Int32)
                                || (o2 is Int64))
                            {
                                n2 = (Int64)o2;
                                if (bException == true)
                                    s2 = Convert.ToString(n2);
                            }
                            else if (o2 is string)
                            {
                                if (bException == true)
                                    s2 = (string)o2;
                                else
                                {
                                    try
                                    {
                                        n2 = Convert.ToInt64((string)o2);
                                    }
                                    catch
                                    {
                                        s2 = (string)o2;
                                        bException = true;
                                        s1 = Convert.ToString(n1);
                                    }
                                }
                            }

                            if (bException == true)
                            {
                                // ����
                                int nMaxLength = Math.Max(s1.Length, s2.Length);
                                s2 = s2.PadLeft(nMaxLength, '0');
                                s1 = s1.PadLeft(nMaxLength, '0');

                                nRet = String.Compare(s1, s2, column.bIgnorCase);
                                if (column.bAsc == false)
                                    nRet = nRet * (-1);
                                if (nRet != 0)
                                    return nRet;
                            }
                            else
                            {

                                Int64 n64Ret = n1 - n2;
                                if (column.bAsc == false)
                                    n64Ret = n64Ret * (-1);
                                if (n64Ret != 0)
                                    return (int)n64Ret;
                            }
                        }
                        else if (column.dataType == DataType.String)
                        {
                            string s1 = "";
                            string s2 = "";


                            if ((o1 is Int32)
                                || (o1 is Int64))
                                s1 = Convert.ToString((Int64)o1);
                            else if (o1 is string)
                                s1 = (string)o1;


                            if ((o2 is Int32)
                                || (o2 is Int64))
                                s2 = Convert.ToString((Int64)o2);
                            else if (o2 is string)
                                s2 = (string)o2;


                            nRet = String.Compare(s1, s2, column.bIgnorCase);
                            if (column.bAsc == false)
                                nRet = nRet * (-1);
                            if (nRet != 0)
                                return nRet;
                        }

#endif

                    } // end of else

                } // end of loop

                return 0;
            }
        }

        #endregion


    }
}
