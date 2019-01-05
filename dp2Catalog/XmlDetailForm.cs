using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.IO;

using DigitalPlatform.Xml;
using DigitalPlatform;

namespace dp2Catalog
{
    public partial class XmlDetailForm : Form
    {
        bool m_bDisplayOriginPage = true;   // �Ƿ���ʾԭʼ��������ҳ
        public bool DisplayOriginPage
        {
            get
            {
                return this.m_bDisplayOriginPage;
            }
            set
            {
                this.m_bDisplayOriginPage = value;

                if (value == false)
                {
                    // TODO: �������ڴ�й©����Ҫ�Ľ�
                    if (this.tabControl_main.TabPages.IndexOf(this.tabPage_originData) != -1)
                        this.tabControl_main.TabPages.Remove(this.tabPage_originData);
                }
                else
                {
                    if (this.tabControl_main.TabPages.IndexOf(this.tabPage_originData) == -1)
                        this.tabControl_main.TabPages.Add(this.tabPage_originData);
                }
            }
        }

        const int WM_LOADSIZE = API.WM_USER + 201;
        const int WM_VERIFY_DATA = API.WM_USER + 204;
        const int WM_FILL_MARCEDITOR_SCRIPT_MENU = API.WM_USER + 205;

        public LoginInfo LoginInfo = new LoginInfo();
        public MainForm MainForm = null;

        DigitalPlatform.Stop stop = null;

        public ISearchForm LinkedSearchForm = null;

        DigitalPlatform.Z3950.Record CurrentRecord = null;

        Encoding CurrentEncoding = Encoding.GetEncoding(936);

        public string AutoDetectedMarcSyntaxOID = "";

        byte[] CurrentTimestamp = null;
        // ���ڱ����¼��·��
        public string SavePath
        {
            get
            {
                return this.textBox_savePath.Text;
            }
            set
            {
                this.textBox_savePath.Text = value;
            }

        }


        public XmlDetailForm()
        {
            InitializeComponent();
        }

        private void XmlDetailForm_Load(object sender, EventArgs e)
        {
            stop = new DigitalPlatform.Stop();
            stop.Register(MainForm.stopManager, true);	// ����������

            Global.FillEncodingList(this.comboBox_originDataEncoding,
                false);

            this.NeedIndentXml = this.MainForm.AppInfo.GetBoolean(
                "xmldetailform",
                "need_indent_xml",
                true);

            API.PostMessage(this.Handle, WM_LOADSIZE, 0, 0);
        }

        private void XmlDetailForm_FormClosing(object sender, FormClosingEventArgs e)
        {

        }

        private void XmlDetailForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (stop != null) // �������
            {
                stop.Unregister();	// ����������
                stop = null;
            }

            SaveSize();

            if (this.MainForm != null && this.MainForm.AppInfo != null)
            {
                this.MainForm.AppInfo.SetBoolean(
        "xmldetailform",
        "need_indent_xml",
        this.NeedIndentXml);
            }
        }

        public int LoadRecord(string strDirection,
            bool bForceFull = false)
        {
            string strError = "";

            if (this.LinkedSearchForm == null)
            {
                strError = "û�й����ļ�����";
                goto ERROR1;
            }

            string strPath = this.textBox_tempRecPath.Text;
            if (String.IsNullOrEmpty(strPath) == true)
            {
                strError = "·��Ϊ��";
                goto ERROR1;
            }

            // �������������
            string strProtocol = "";
            string strResultsetName = "";
            string strIndex = "";

            int nRet = MarcDetailForm.ParsePath(strPath,
                out strProtocol,
                out strResultsetName,
                out strIndex,
                out strError);
            if (nRet == -1)
            {
                strError = "����·�� '" + strPath + "' �ַ��������з�������: " + strError;
                goto ERROR1;
            }


            if (strProtocol != this.LinkedSearchForm.CurrentProtocol)
            {
                strError = "��������Э���Ѿ������ı�";
                goto ERROR1;
            }

            if (strResultsetName != this.LinkedSearchForm.CurrentResultsetPath)
            {
                strError = "������Ѿ������ı�";
                goto ERROR1;
            }

            int index = 0;

            index = Convert.ToInt32(strIndex) - 1;


            if (strDirection == "prev")
            {
                index--;
                if (index < 0)
                {
                    strError = "��ͷ";
                    goto ERROR1;
                }
            }
            else if (strDirection == "current")
            {
            }
            else if (strDirection == "next")
            {
                index++;
            }
            else
            {
                strError = "����ʶ���strDirection����ֵ '" + strDirection + "'";
                goto ERROR1;
            }

            return LoadRecord(this.LinkedSearchForm, index, bForceFull);
        ERROR1:
            MessageBox.Show(this, strError);
            return -1;
        }

        // װ��XML��¼
        public int LoadRecord(ISearchForm searchform,
            int index,
            bool bForceFullElementSet = false)
        {
            string strError = "";
            string strMARC = "";

            this.LinkedSearchForm = searchform;
            this.SavePath = "";

            DigitalPlatform.Z3950.Record record = null;
            Encoding currentEncoding = null;

            this.CurrentRecord = null;

            byte[] baTimestamp = null;
            string strSavePath = "";
            string strOutStyle = "";
            LoginInfo logininfo = null;
            long lVersion = 0;

            string strXmlFragment = "";

            string strParameters = "hilight_browse_line";
            if (bForceFullElementSet == true)
                strParameters += ",force_full";

            int nRet = searchform.GetOneRecord(
                "xml",
                index,  // ������ֹ
                "index:" + index.ToString(),
                strParameters, // true,
                out strSavePath,
                out strMARC,
                out strXmlFragment,
                out strOutStyle,
                out baTimestamp,
                out lVersion,
                out record,
                out currentEncoding,
                out logininfo,
                out strError);
            if (nRet == -1)
                goto ERROR1;


            this.LoginInfo = logininfo;

            this.CurrentTimestamp = baTimestamp;
            this.SavePath = strSavePath;
            this.CurrentEncoding = currentEncoding;


            // �滻����0x0a
            strMARC = strMARC.Replace("\r", "");
            strMARC = strMARC.Replace("\n", "\r\n");

            // װ��XML�༭��
            // this.textBox_xml.Text = strMARC;
            this.PlainText = strMARC;   // ���Զ�����
            this.textBox_xml.Select(0, 0);

            // װ��XMLֻ��Web�ؼ�
            {
                string strTempFileName = MainForm.DataDir + "\\xml.xml";

                // SUTRS
                if (record.m_strSyntaxOID == "1.2.840.10003.5.101")
                    strTempFileName = MainForm.DataDir + "\\xml.txt";

                using (Stream stream = File.Create(strTempFileName))
                {
                    // д��xml����
                    byte[] buffer = Encoding.UTF8.GetBytes(strMARC);
                    stream.Write(buffer, 0, buffer.Length);
                }

                this.webBrowser_xml.Navigate(strTempFileName);
            }

            this.CurrentRecord = record;
            if (this.CurrentRecord != null && this.DisplayOriginPage == true)
            {
                // װ������Ʊ༭��
                this.binaryEditor_originData.SetData(
                    this.CurrentRecord.m_baRecord);

                // װ��ԭʼ�ı�
                nRet = this.SetOriginText(this.CurrentRecord.m_baRecord,
                    this.CurrentEncoding,
                    out strError);
                if (nRet == -1)
                {
                    this.textBox_originData.Text = strError;
                }

                // ���ݿ���
                this.textBox_originDatabaseName.Text = this.CurrentRecord.m_strDBName;

                // record syntax OID
                this.textBox_originMarcSyntaxOID.Text = this.CurrentRecord.m_strSyntaxOID;
            }

            // ����·��
            string strPath = searchform.CurrentProtocol + ":"
                + searchform.CurrentResultsetPath
                + "/" + (index + 1).ToString();

            this.textBox_tempRecPath.Text = strPath;
            this.textBox_xml.Focus();
            return 0;
        ERROR1:
            MessageBox.Show(this, strError);
            return -1;
        }

        int SetOriginText(byte[] baOrigin,
    Encoding encoding,
    out string strError)
        {
            strError = "";

            if (encoding == null)
            {
                int nRet = this.MainForm.GetEncoding(this.comboBox_originDataEncoding.Text,
                    out encoding,
                    out strError);
                if (nRet == -1)
                    return -1;
            }
            else
            {
                this.comboBox_originDataEncoding.Text = GetEncodingForm.GetEncodingName(this.CurrentEncoding);

            }

            this.textBox_originData.Text = encoding.GetString(baOrigin);

            return 0;
        }

        // װ�� XML �� textbox ��ʱ����Ҫ����Ч��ô?
        bool NeedIndentXml
        {
            get
            {
                return this.toolStripButton_indentXmlText.Checked;
            }
            set
            {
                this.toolStripButton_indentXmlText.Checked = value;
            }
        }

        public string PlainText
        {
            get
            {
                return this.textBox_xml.Text;
            }
            set
            {
                if (this.NeedIndentXml == false)
                {
                    this.textBox_xml.Text = value;
                    return;
                }
                string strError = "";
                string strOutXml = "";
                int nRet = DomUtil.GetIndentXml(value,
                    out strOutXml,
                    out strError);
                if (nRet == -1)
                {
                    // ���ܲ����� XML
                    this.textBox_xml.Text = value;
                    return;
                }

                this.textBox_xml.Text = strOutXml;
            }
        }

        private void toolStripButton_indentXmlText_Click(object sender, EventArgs e)
        {
            if (this.toolStripButton_indentXmlText.Checked == true)
            {
                string strError = "";
                string strOutXml = "";
                int nRet = DomUtil.GetIndentXml(this.textBox_xml.Text,
                    out strOutXml,
                    out strError);
                if (nRet == -1)
                {
                    MessageBox.Show(this, strError);
                    return;
                }

                this.textBox_xml.Text = strOutXml;
            }
        }

        private void XmlDetailForm_Activated(object sender, EventArgs e)
        {
            MainForm.toolButton_prev.Enabled = true;
            MainForm.toolButton_next.Enabled = true;

            MainForm.toolButton_nextBatch.Enabled = false;
            MainForm.toolButton_getAllRecords.Enabled = false;

        }

        protected override void DefWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_LOADSIZE:
                    LoadSize();
                    return;

            }
            base.DefWndProc(ref m);
        }

        public void LoadSize()
        {
            // ���ô��ڳߴ�״̬
            MainForm.AppInfo.LoadMdiChildFormStates(this,
                "mdi_form_state",
                SizeStyle.All,
                MainForm.DefaultMdiWindowWidth,
                MainForm.DefaultMdiWindowHeight);


            // ���splitContainer_originDataMain��״̬
            int nValue = MainForm.AppInfo.GetInt(
            "xmldetailform",
            "splitContainer_originDataMain",
            -1);
            if (nValue != -1)
                this.splitContainer_originDataMain.SplitterDistance = nValue;

            try
            {
                this.tabControl_main.SelectedIndex = this.MainForm.AppInfo.GetInt(
                    "xmldetailform",
                    "active_page",
                    0);
            }
            catch
            {
            }
        }

        public void SaveSize()
        {
            if (this.MainForm != null && this.MainForm.AppInfo != null)
            {
                MainForm.AppInfo.SaveMdiChildFormStates(this,
                    "mdi_form_state");

                // ����splitContainer_originDataMain��״̬
                MainForm.AppInfo.SetInt(
                    "xmldetailform",
                    "splitContainer_originDataMain",
                    this.splitContainer_originDataMain.SplitterDistance);

                this.MainForm.AppInfo.SetInt(
                    "xmldetailform",
                    "active_page",
                    this.tabControl_main.SelectedIndex);
            }
        }
    }
}