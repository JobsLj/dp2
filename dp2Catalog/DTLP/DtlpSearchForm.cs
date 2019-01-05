using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;

using System.Diagnostics;
using System.IO;
using System.Collections;

// using System.Reflection;
//using DigitalPlatform.Script;

using DigitalPlatform;
using DigitalPlatform.DTLP;
using DigitalPlatform.Text;
using DigitalPlatform.Xml;
using DigitalPlatform.Marc;
using DigitalPlatform.MarcDom;
//using DigitalPlatform.IO;
using DigitalPlatform.GUI;

namespace dp2Catalog
{
    public partial class DtlpSearchForm : MyForm, ISearchForm
    {
        // ����������к�����
        SortColumns SortColumns = new SortColumns();

        public string BinDir = "";

#if NO
        public MainForm MainForm = null;

        DigitalPlatform.Stop stop = null;
#endif

        public DtlpChannelArray DtlpChannels = new DtlpChannelArray();
        public DtlpChannel DtlpChannel = null;	// ����ʹ��һ��ͨ��
        Hashtable AccountTable = new Hashtable();

        // ��ǰ��������
        string strCurrentTargetPath = "";
        string strCurrentQueryWord = "";

        const int WM_LOADSIZE = API.WM_USER + 201;

        // ��ǰȱʡ�ı��뷽ʽ
        Encoding CurrentEncoding = Encoding.GetEncoding("gb2312");

        /// <summary>
        /// ���������ź�
        /// </summary>
        public AutoResetEvent EventLoadFinish = new AutoResetEvent(false);

        public DtlpSearchForm()
        {
            InitializeComponent();

            DtlpChannels.Idle -= new DtlpIdleEventHandler(DtlpChannels_Idle);
            DtlpChannels.Idle += new DtlpIdleEventHandler(DtlpChannels_Idle);
        }

        void DtlpChannels_Idle(object sender, DtlpIdleEventArgs e)
        {
            e.bDoEvents = true;
        }

        #region ISearchForm �ӿں���

        // ���󡢴����Ƿ���Ч?
        public bool IsValid()
        {
            if (this.IsDisposed == true)
                return false;

            return true;
        }

        public string CurrentProtocol
        {
            get
            {
                return "dtlp";
            }
        }

        public string CurrentResultsetPath
        {
            get
            {
                return this.strCurrentTargetPath
                    + this.strCurrentQueryWord
                    + "/default";
            }
        }


        // ˢ��һ��MARC��¼
        // return:
        //      -2  ��֧��
        //      -1  error
        //      0   ��ش����Ѿ����٣�û�б�Ҫˢ��
        //      1   �Ѿ�ˢ��
        //      2   �ڽ������û���ҵ�Ҫˢ�µļ�¼
        public int RefreshOneRecord(
            string strPathParam,
            string strAction,
            out string strError)
        {
            strError = "��δʵ��";

            return -2;
        }

        public int SyncOneRecord(string strPath,
            ref long lVersion,
            ref string strSyntax,
            ref string strMARC,
            out string strError)
        {
            strError = "";
            return 0;
        }

        public int GetOneRecord(
            string strStyle,
            int nTest,
            string strPathParam,
            string strParameters,   // bool bHilightBrowseLine,
            out string strSavePath,
            out string strMARC,
            out string strXmlFragment,
            out string strOutStyle,
            out byte[] baTimestamp,
            out long lVersion,
            out DigitalPlatform.Z3950.Record record,
            out Encoding currrentEncoding,
            out LoginInfo logininfo,
            out string strError)
        {
            strXmlFragment = "";
            strMARC = "";
            record = null;
            strError = "";
            currrentEncoding = this.CurrentEncoding;
            baTimestamp = null;
            strSavePath = "";
            strOutStyle = "marc";
            logininfo = new LoginInfo();
            lVersion = 0;

            if (strStyle != "marc")
            {
                strError = "DtlpSearchFormֻ֧�ֻ�ȡMARC��ʽ��¼";
                return -1;
            }

            int nRet = 0;

            int index = -1;
            string strPath = "";
            string strDirection = "";
            nRet = Global.ParsePathParam(strPathParam,
                out index,
                out strPath,
                out strDirection,
                out strError);
            if (nRet == -1)
                return -1;

            if (index == -1)
            {
                string strOutputPath = "";
                nRet = InternalGetOneRecord(
                    strStyle,
                    strPath,
                    strDirection,
                    out strMARC,
                    out strOutputPath,
                    out strOutStyle,
                    out baTimestamp,
                    out record,
                    out currrentEncoding,
                    out strError);
                if (string.IsNullOrEmpty(strOutputPath) == false)
                    strSavePath = this.CurrentProtocol + ":" + strOutputPath;
                return nRet;
            }


            bool bHilightBrowseLine = StringUtil.IsInList("hilight_browse_line", strParameters);

            // int nRet = 0;

            // int nStyle = DtlpChannel.XX_STYLE; // �����ϸ��¼

            if (index >= this.listView_browse.Items.Count)
            {
                // ������������жϹ���������Դ�����������

                strError = "Խ�������β��";
                return -1;
            }

            ListViewItem curItem = this.listView_browse.Items[index];

            if (bHilightBrowseLine == true)
            {
                // �޸�listview�������ѡ��״̬
                for (int i = 0; i < this.listView_browse.SelectedItems.Count; i++)
                {
                    this.listView_browse.SelectedItems[i].Selected = false;
                }

                curItem.Selected = true;
                curItem.EnsureVisible();
            }

            strPath = curItem.Text;

            // ��·��ת��Ϊ�ں˿��Խ��ܵ�������̬
            strPath = DigitalPlatform.DTLP.Global.ModifyDtlpRecPath(strPath,
                "ctlno");

            strSavePath = this.CurrentProtocol + ":" + strPath;

            /*

            byte[] baPackage;
            nRet = this.DtlpChannel.Search(strPath,
                nStyle,
                out baPackage);
            if (nRet == -1)
            {
                int errorcode = this.DtlpChannel.GetLastErrno();
                strError = "��������:\r\n"
                    + "����ʽ: " + strPath + "\r\n"
                    + "������: " + errorcode + "\r\n"
                    + "������Ϣ: " + this.DtlpChannel.GetErrorString(errorcode) + "\r\n";
                goto ERROR1;
            }

            Encoding encoding = this.DtlpChannel.GetPathEncoding(strPath);
            Package package = new Package();
            package.LoadPackage(baPackage,
                encoding);
            nRet = package.Parse(PackageFormat.Binary);
            if (nRet == -1)
            {
                strError = "Package::Parse() error";
                goto ERROR1;
            }

            byte[] content = null;
            nRet = package.GetFirstBin(out content);
            if (nRet == -1)
            {
                strError = "Package::GetFirstBin() error";
                goto ERROR1;
            }

            if (content == null
                || content.Length < 9)
            {
                strError = "content length < 9";
                goto ERROR1;
            }

            baTimestamp = new byte[9];
            Array.Copy(content, baTimestamp, 9);

            byte[] marc = new byte[content.Length - 9];
            Array.Copy(content, 
                9,
                marc,
                0,
                content.Length - 9);

            // strMARC = this.CurrentEncoding.GetString(marc);
            strMARC = encoding.GetString(marc);

            // ȥ���������������29�ַ�����0�ַ�
            // 2008/3/11
            int nDelta = 0;
            for (int i = strMARC.Length - 1; i > 24; i--)
            {
                char ch = strMARC[i];
                if (ch == 0 || ch == 29)
                    nDelta++;
                else
                    break;
            }

            if (nDelta > 0)
                strMARC = strMARC.Substring(0, strMARC.Length - nDelta);

            // �Զ�ʶ��MARC��ʽ
            string strOutMarcSyntax = "";
            // ̽���¼��MARC��ʽ unimarc / usmarc / reader
            nRet = MarcUtil.DetectMarcSyntax(strMARC,
                out strOutMarcSyntax);
            if (strOutMarcSyntax == "")
                strOutMarcSyntax = "unimarc";

            record = new DigitalPlatform.Z3950.Record();
            if (strOutMarcSyntax == "unimarc" || strOutMarcSyntax == "")
                record.m_strSyntaxOID = "1.2.840.10003.5.1";
            else if (strOutMarcSyntax == "usmarc")
                record.m_strSyntaxOID = "1.2.840.10003.5.10";
            else if (strOutMarcSyntax == "dt1000reader")
                record.m_strSyntaxOID = "1.2.840.10003.5.dt1000reader";
            else
            {
                // TODO: ���Գ��ֲ˵�ѡ��
            }
             * */
            {
                string strOutputPath = "";

                nRet = InternalGetOneRecord(
                    strStyle,
                    strPath,
                    "current",
                    out strMARC,
                    out strOutputPath,
                    out strOutStyle,
                    out baTimestamp,
                    out record,
                    out currrentEncoding,
                    out strError);
                if (string.IsNullOrEmpty(strOutputPath) == false)
                    strSavePath = this.CurrentProtocol + ":" + strOutputPath;
                return nRet;
            }
        ERROR1:
            return -1;
        }

        #endregion

        void DoNewStop(object sender, StopEventArgs e)
        {
            Stop current_stop = (Stop)sender;
            if (current_stop == null)
                return;
            DtlpChannel channel = (DtlpChannel)current_stop.Tag;
            if (channel == null)
                return;

            channel.Cancel();
        }

        // ���һ��MARC��¼
        // ע�����this.DtlpChannel��ռ�ã����������µ�ͨ��
        // TODO: ��δ����������ͨ��ʱ������Stop�Ŀ���
        // parameters:
        //      strPath ��¼·������ʽΪ"localhost/����ͼ��/ctlno/1"
        //      strDirection    ����Ϊ prev/next/current֮һ��current����ȱʡ��
        //      strOutputPath   [out]���ص�ʵ��·������ʽ��strPath��ͬ��
        // return:
        //      -1  error ����not found
        //      0   found
        //      1   Ϊ��ϼ�¼
        int InternalGetOneRecord(
            string strStyle,
            string strPath,
            string strDirection,
            out string strMARC,
            out string strOutputPath,
            out string strOutStyle,
            out byte[] baTimestamp,
            out DigitalPlatform.Z3950.Record record,
            out Encoding currrentEncoding,
            out string strError)
        {
            strMARC = "";
            record = null;
            strError = "";
            currrentEncoding = this.CurrentEncoding;
            baTimestamp = null;
            strOutStyle = "marc";
            strOutputPath = ""; // TODO: ��Ҫ�ο�dp1batch�����outputpath�ķ���

            if (strStyle != "marc")
            {
                strError = "DtlpSearchFormֻ֧�ֻ�ȡMARC��ʽ��¼";
                return -1;
            }

            int nRet = 0;

            int nStyle = DtlpChannel.XX_STYLE; // �����ϸ��¼

            if (strDirection == "prev")
                nStyle |= DtlpChannel.PREV_RECORD;
            else if (strDirection == "next")
                nStyle |= DtlpChannel.NEXT_RECORD;

            /*
            // ��·��ת��Ϊ�ں˿��Խ��ܵ�������̬
            string strPath = DigitalPlatform.DTLP.Global.ModifyDtlpRecPath(strPath,
                "ctlno");
             * */
            Stop temp_stop = this.stop;
            DtlpChannel channel = null;

            bool bNewChannel = false;
            if (this.m_nInSearching == 0)
                channel = this.DtlpChannel;
            else
            {
                channel = this.DtlpChannels.CreateChannel(0);
                bNewChannel = true;

                temp_stop = new Stop();
                temp_stop.Tag = channel;
                temp_stop.Register(MainForm.stopManager, true);	// ����������

                temp_stop.OnStop += new StopEventHandler(this.DoNewStop);
                temp_stop.Initial("���ڳ�ʼ���������� ...");
                temp_stop.BeginLoop();

            }

                byte[] baPackage = null;
            Encoding encoding = null;
            try
            {

                nRet = channel.Search(strPath,
                    nStyle,
                    out baPackage);
                if (nRet == -1)
                {
                    int errorcode = channel.GetLastErrno();

                    if (errorcode == DtlpChannel.GL_NOTEXIST
                        && (strDirection == "prev" || strDirection == "next"))
                    {
                        if (strDirection == "prev")
                            strError = "��ͷ";
                        else if (strDirection == "next")
                            strError = "��β";
                        goto ERROR1;
                    }
                    strError = "��������:\r\n"
                        + "����ʽ: " + strPath + "\r\n"
                        + "������: " + errorcode + "\r\n"
                        + "������Ϣ: " + channel.GetErrorString(errorcode) + "\r\n";
                    goto ERROR1;
                }

                encoding = channel.GetPathEncoding(strPath);
            }
            finally
            {
                if (bNewChannel == true)
                {
                    temp_stop.EndLoop();
                    temp_stop.OnStop -= new StopEventHandler(this.DoNewStop);
                    temp_stop.Initial("");

                    this.DtlpChannels.DestroyChannel(channel);
                    channel = null;


                    temp_stop.Unregister();	// ����������
                    temp_stop = null;
                }
            }
            Package package = new Package();
            package.LoadPackage(baPackage,
                encoding);
            nRet = package.Parse(PackageFormat.Binary);
            if (nRet == -1)
            {
                strError = "Package::Parse() error";
                goto ERROR1;
            }

            strOutputPath = package.GetFirstPath();

            byte[] content = null;
            nRet = package.GetFirstBin(out content);
            if (nRet == -1)
            {
                strError = "Package::GetFirstBin() error";
                goto ERROR1;
            }

            if (content == null
                || content.Length < 9)
            {
                strError = "content length < 9";
                goto ERROR1;
            }

            baTimestamp = new byte[9];
            Array.Copy(content, baTimestamp, 9);

            byte[] marc = new byte[content.Length - 9];
            Array.Copy(content,
                9,
                marc,
                0,
                content.Length - 9);

            // strMARC = this.CurrentEncoding.GetString(marc);
            strMARC = encoding.GetString(marc);

            // ȥ���������������29�ַ�����0�ַ�
            // 2008/3/11
            int nDelta = 0;
            for (int i = strMARC.Length - 1; i > 24; i--)
            {
                char ch = strMARC[i];
                if (ch == 0 || ch == 29)
                    nDelta++;
                else
                    break;
            }

            if (nDelta > 0)
                strMARC = strMARC.Substring(0, strMARC.Length - nDelta);

            // �Զ�ʶ��MARC��ʽ
            string strOutMarcSyntax = "";
            // ̽���¼��MARC��ʽ unimarc / usmarc / reader
            // return:
            //      0   û��̽�������strMarcSyntaxΪ��
            //      1   ̽�������
            nRet = MarcUtil.DetectMarcSyntax(strMARC,
                out strOutMarcSyntax);
            if (strOutMarcSyntax == "")
                strOutMarcSyntax = "unimarc";

            record = new DigitalPlatform.Z3950.Record();
            if (strOutMarcSyntax == "unimarc" || strOutMarcSyntax == "")
                record.m_strSyntaxOID = "1.2.840.10003.5.1";
            else if (strOutMarcSyntax == "usmarc")
                record.m_strSyntaxOID = "1.2.840.10003.5.10";
            else if (strOutMarcSyntax == "dt1000reader")
                record.m_strSyntaxOID = "1.2.840.10003.5.dt1000reader";
            else
            {
                /*
                strError = "δ֪��MARC syntax '" + strOutMarcSyntax + "'";
                goto ERROR1;
                 * */
                // TODO: ���Գ��ֲ˵�ѡ��
            }

            return 0;
        ERROR1:
            return -1;
        }

        public int GetAccessPoint(
            string strPath,
            string strMARC,
            out List<string> results,
            out string strError)
        {
            strError = "";
                    // ���һ����¼�ļ�����
            return this.DtlpChannel.GetAccessPoint(strPath,
                strMARC,
                out results,
                out strError);
        }

        private void DtlpSearchForm_Load(object sender, EventArgs e)
        {
            EventLoadFinish.Reset();

            this.BinDir = Environment.CurrentDirectory;

#if NO
            stop = new DigitalPlatform.Stop();
            stop.Register(MainForm.stopManager, true);	// ����������
#endif

            // ��ʼ��ChannelArray
            DtlpChannels.appInfo = MainForm.AppInfo;
            DtlpChannels.AskAccountInfo += new AskDtlpAccountInfoEventHandle(channelArray_AskAccountInfo);
            /*
            channelArray.procAskAccountInfo = new Delegate_AskAccountInfo(
                this.AskAccountInfo);
             * */

            // ׼��Ψһ��ͨ��
            if (this.DtlpChannel == null)
            {
                this.DtlpChannel = DtlpChannels.CreateChannel(0);
            }

            this.dtlpResDirControl1.channelarray = DtlpChannels;
            dtlpResDirControl1.Channel = this.DtlpChannel;
            dtlpResDirControl1.Stop = this.stop;

            /*
            dtlpResDirControl1.procItemSelected = new Delegate_ItemSelected(
                this.ItemSelected);
            dtlpResDirControl1.procItemText = new Delegate_ItemText(
                this.ItemText);
             * */
            dtlpResDirControl1.FillSub(null);

            /*
             * ��Ҫ�첽ִ�У����ⴰ�ڳ�ʱ��򲻿�
            string strLastTargetPath = MainForm.applicationInfo.GetString(
                "dtlpsearchform",
                "last_targetpath",
                "");
            if (String.IsNullOrEmpty(strLastTargetPath) == false)
            {
                this.dtlpResDirControl1.SelectedPath = strLastTargetPath;
            }*/

            // �����ϴα����·��չ��resdircontrol��
            string strResDirPath = this.MainForm.AppInfo.GetString(
                "dtlpsearchform",
                "last_targetpath",
                "");
            if (String.IsNullOrEmpty(strResDirPath) == false)
            {
                object[] pList = { strResDirPath };

                this.BeginInvoke(new Delegate_ExpandResDir(ExpandResDir),
                    pList);
            }
            else
            {
                this.EventLoadFinish.Set();
            }

            string strQueryWord = MainForm.AppInfo.GetString(
                "dtlpsearchform",
                "query_content",
                "");
            this.textBox_queryWord.Text = strQueryWord;

            API.PostMessage(this.Handle, WM_LOADSIZE, 0, 0);
        }

        /// <summary>
        /// �ȴ�װ�ؽ���
        /// </summary>
        public void WaitLoadFinish()
        {
            for (; ; )
            {
                Application.DoEvents();
                bool bRet = this.EventLoadFinish.WaitOne(10, true);
                if (bRet == true)
                    break;
            }
        }

        public delegate void Delegate_ExpandResDir(string strResDirPath);

        void ExpandResDir(string strLastTargetPath)
        {
            this.Update();

            this.EnableControls(false);

            // չ����ָ���Ľڵ�
            if (String.IsNullOrEmpty(strLastTargetPath) == false)
            {
                Debug.Assert(this.dtlpResDirControl1.PathSeparator == "\\", "");
                this.dtlpResDirControl1.SelectedPath1 = strLastTargetPath;
            }

            this.EnableControls(true);

            this.EventLoadFinish.Set();
        }

        void channelArray_AskAccountInfo(object sender,
            AskDtlpAccountInfoEventArgs e)
        {
            e.Owner = null;
            e.UserName = "";
            e.Password = "";

            LoginDlg dlg = new LoginDlg();
            GuiUtil.SetControlFont(dlg, this.Font);

            AccountItem item = (AccountItem)AccountTable[e.Path];
            if (item == null)
            {
                item = new AccountItem();
                AccountTable.Add(e.Path, item);

                // �������ļ��еõ�ȱʡ�˻�
                item.UserName = MainForm.AppInfo.GetString(
                    "preference",
                    "defaultUserName",
                    "public");
                item.Password = MainForm.AppInfo.GetString(
                    "preference",
                    "defaultPassword",
                    "");
            }

            dlg.textBox_serverAddr.Text = e.Path;
            dlg.textBox_userName.Text = item.UserName;
            dlg.textBox_password.Text = item.Password;

            // �ȵ�¼һ����˵
            {
                byte[] baResult = null;
                int nRet = e.Channel.API_ChDir(dlg.textBox_userName.Text,
                    dlg.textBox_password.Text,
                    e.Path,
                    out baResult);

                // ��¼�ɹ�
                if (nRet > 0)
                {
                    e.Result = 2;
                    return;
                }
            }


            dlg.StartPosition = FormStartPosition.CenterScreen;
            dlg.ShowDialog(this);

            if (dlg.DialogResult == DialogResult.OK)
            {
                item.UserName = dlg.textBox_userName.Text;
                item.Password = dlg.textBox_password.Text;

                e.UserName = dlg.textBox_userName.Text;
                e.Password = dlg.textBox_password.Text;
                e.Owner = this;
                e.Result = 1;
                return;
            }

            e.Result = 0;
            return;
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


            // ���splitContainer_main��״̬
            int nValue = MainForm.AppInfo.GetInt(
            "dtlpsearchform",
            "splitContainer_main",
            -1);
            if (nValue != -1)
            {
                try
                {
                    this.splitContainer_main.SplitterDistance = nValue;
                }
                catch
                {
                }
            }

            // ���splitContainer_up��״̬
            nValue = MainForm.AppInfo.GetInt(
            "dtlpsearchform",
            "splitContainer_up",
            -1);
            if (nValue != -1)
            {
                try
                {
                    this.splitContainer_up.SplitterDistance = nValue;
                }
                catch
                {
                }
            }

            // 2008/3/24
            if (this.dtlpResDirControl1.SelectedNode != null)
                this.dtlpResDirControl1.SelectedNode.EnsureVisible();
        }

        public void SaveSize()
        {
            if (this.MainForm != null && this.MainForm.AppInfo != null)
            {
                MainForm.AppInfo.SaveMdiChildFormStates(this,
                    "mdi_form_state");

                // ����splitContainer_main��״̬
                MainForm.AppInfo.SetInt(
                    "dtlpsearchform",
                    "splitContainer_main",
                    this.splitContainer_main.SplitterDistance);
                // ����splitContainer_up��״̬
                MainForm.AppInfo.SetInt(
                    "dtlpsearchform",
                    "splitContainer_up",
                    this.splitContainer_up.SplitterDistance);
            }
        }

        private void DtlpSearchForm_FormClosing(object sender, FormClosingEventArgs e)
        {
#if NO
            if (stop != null)
            {
                if (stop.State == 0)    // 0 ��ʾ���ڴ���
                {
                    MessageBox.Show(this, "���ڹرմ���ǰֹͣ���ڽ��еĳ�ʱ������");
                    e.Cancel = true;
                    return;
                }
            }
#endif
        }

        private void DtlpSearchForm_FormClosed(object sender, FormClosedEventArgs e)
        {
#if NO
            if (stop != null) // �������
            {
                stop.Style = StopStyle.None;    // ��Ҫǿ���ж�
                stop.DoStop();

                stop.Unregister();	// �������������
                stop = null;
            }
#endif

            if (this.MainForm != null && this.MainForm.AppInfo != null)
            {
                Debug.Assert(this.dtlpResDirControl1.PathSeparator == "\\", "");
                string strLastTargetPath = TreeViewUtil.GetPath(this.dtlpResDirControl1.SelectedNode,
                    '\\');
                MainForm.AppInfo.SetString(
                    "dtlpsearchform",
                    "last_targetpath",
                    strLastTargetPath);

                MainForm.AppInfo.SetString(
                    "dtlpsearchform",
                    "query_content",
                    this.textBox_queryWord.Text);
            }

            DtlpChannels.AskAccountInfo -= new AskDtlpAccountInfoEventHandle(channelArray_AskAccountInfo);

            SaveSize();
        }

        void DoStop(object sender, StopEventArgs e)
        {
            if (this.DtlpChannel != null)
                this.DtlpChannel.Cancel();
        }

        // ��������¼����
        // ע��ʹ�����е�this.DplpChannel
        int GetOneBrowseRecord(
            string strPath,
            out string[] cols,
            out string strError)
        {
            strError = "";
            cols = null;

            int nRet = 0;

            int nStyle = DtlpChannel.JH_STYLE; // ��ü򻯼�¼

            byte[] baPackage;
            nRet = this.DtlpChannel.Search(strPath,
                nStyle,
                out baPackage);
            if (nRet == -1)
            {
                strError = "Search() path '" + strPath + "' ʱ��������: " + this.DtlpChannel.GetErrorString();
                goto ERROR1;
            }

            Package package = new Package();
            package.LoadPackage(baPackage,
                this.DtlpChannel.GetPathEncoding(strPath));
            nRet = package.Parse(PackageFormat.String);
            if (nRet == -1)
            {
                strError = "Package::Parse() error";
                goto ERROR1;
            }

            string strContent = package.GetFirstContent();

            if (String.IsNullOrEmpty(strContent) == false)
            {
                cols = strContent.Split(new char[] {'\t'});
            }

            return 0;
        ERROR1:
            return -1;
        }

        int m_nInSearching = 0; // ��ʾthis.DtlpChannel�Ƿ�ռ��

        /*
����δ����Ľ����߳��쳣: 
Type: System.ObjectDisposedException
Message: �޷��������ͷŵĶ���
������:��System.Net.Sockets.NetworkStream����
Stack:
�� System.Net.Sockets.NetworkStream.EndRead(IAsyncResult asyncResult)
�� DigitalPlatform.DTLP.HostEntry.RecvTcpPackage(Byte[]& baPackage, Int32& nLen, Int32& nErrorNo)
�� DigitalPlatform.DTLP.DtlpChannel.API_Search(String strPath, Int32 lStyle, Byte[]& baResult)
�� DigitalPlatform.DTLP.DtlpChannel.Search(String strPath, Int32 lStyle, Byte[]& baResult)
�� dp2Catalog.DtlpSearchForm.GetOneBrowseRecord(String strPath, String[]& cols, String& strError)
�� dp2Catalog.DtlpSearchForm.FillBrowseList(Package package, String& strError)
�� dp2Catalog.DtlpSearchForm.DoSearch()
�� dp2Catalog.MainForm.toolButton_search_Click(Object sender, EventArgs e)
�� System.Windows.Forms.ToolStripItem.HandleClick(EventArgs e)
�� System.Windows.Forms.ToolStripItem.HandleMouseUp(MouseEventArgs e)
�� System.Windows.Forms.ToolStrip.OnMouseUp(MouseEventArgs mea)
�� System.Windows.Forms.Control.WmMouseUp(Message& m, MouseButtons button, Int32 clicks)
�� System.Windows.Forms.Control.WndProc(Message& m)
�� System.Windows.Forms.ToolStrip.WndProc(Message& m)
�� System.Windows.Forms.NativeWindow.Callback(IntPtr hWnd, Int32 msg, IntPtr wparam, IntPtr lparam)

         * */
        // ����
        public int DoSearch()
        {
            string strError = "";
            int nRet = 0;

            this._processing++;
            try
            {
                byte[] baNext = null;
                int nStyle = DtlpChannel.CTRLNO_STYLE;

                // nStyle |=  Channel.JH_STYLE;    // ��ü򻯼�¼

                string strPath = "";

                if ((this.dtlpResDirControl1.SelectedMask & DtlpChannel.TypeStdbase) != 0)
                {
                    this.strCurrentTargetPath = this.textBox_resPath.Text + "//";
                }
                else
                {
                    this.strCurrentTargetPath = this.textBox_resPath.Text + "/";
                }
                this.strCurrentQueryWord = this.textBox_queryWord.Text;

                strPath = this.strCurrentTargetPath + this.strCurrentQueryWord;

                this.listView_browse.Items.Clear();
                EnableControls(false);

                stop.OnStop += new StopEventHandler(this.DoStop);
                stop.SetMessage("��ʼ���� ...");
                stop.BeginLoop();

                this.Update();
                this.MainForm.Update();

                this.m_nInSearching++;
                /*
                this.listView_browse.ListViewItemSorter = null; // ��ʱ������������
                 * */

                try
                {
                    int nDupCount = 0;
                    this.listView_browse.Focus();   // ����Excape�ж�

                    bool bFirst = true;       // ��һ�μ���
                    while (true)
                    {
                        Application.DoEvents();	// ���ý������Ȩ

                        if (stop != null)
                        {
                            if (stop.State != 0)
                            {
                                strError = "�û��ж�";
                                goto ERROR1;
                            }
                        }

                        Encoding encoding = this.DtlpChannel.GetPathEncoding(strPath);

                        this.CurrentEncoding = encoding;    // ��������

                        byte[] baPackage;
                        if (bFirst == true)
                        {
                            stop.SetMessage(listView_browse.Items.Count.ToString() + " ȥ��:" + nDupCount.ToString() + " " + "���ڼ��� " + strPath);
                            nRet = this.DtlpChannel.Search(strPath,
                                nStyle,
                                out baPackage);
                        }
                        else
                        {
                            stop.SetMessage(listView_browse.Items.Count.ToString() + " ȥ��:" + nDupCount.ToString() + " " + "���ڼ��� " + strPath + " " + encoding.GetString(baNext));
                            nRet = this.DtlpChannel.Search(strPath,
                                baNext,
                                nStyle,
                                out baPackage);
                        }
                        if (nRet == -1)
                        {
                            int errorcode = this.DtlpChannel.GetLastErrno();
                            if (errorcode == DtlpChannel.GL_NOTEXIST)
                            {
                                /*
                                if (bFirst == true)
                                    break;
                                 * */
                                break;
                            }
                            strError = "��������:\r\n"
                                + "����ʽ: " + strPath + "\r\n"
                                + "������: " + errorcode + "\r\n"
                                + "������Ϣ: " + this.DtlpChannel.GetErrorString(errorcode) + "\r\n";
                            goto ERROR1;
                        }

                        bFirst = false;

                        Package package = new Package();
                        package.LoadPackage(baPackage,
                            encoding/*this.Channel.GetPathEncoding(strPath)*/);
                        // nRet = package.Parse(PackageFormat.String);

                        nRet = package.Parse(PackageFormat.String);
                        if (nRet == -1)
                        {
                            strError = "Package::Parse() error";
                            goto ERROR1;
                        }

                        ///
                        nRet = FillBrowseList(package,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;

                        nDupCount += nRet;

                        if (package.ContinueString != "")
                        {
                            nStyle |= DtlpChannel.CONT_RECORD;
                            baNext = package.ContinueBytes;
                        }
                        else
                        {
                            break;
                        }

                    }

                    this.textBox_resultInfo.Text = "���м�¼ " + this.listView_browse.Items.Count.ToString() + " ��";
                }

                finally
                {
                    this.m_nInSearching--;
                    try
                    {
                        stop.EndLoop();
                        stop.OnStop -= new StopEventHandler(this.DoStop);
                        stop.Initial("");

                        EnableControls(true);

                        /*
                        // �ṩ��������
                        this.listView_browse.ListViewItemSorter = new ListViewBrowseItemComparer();
                         * */

                    }
                    catch { }

                }

                if (this.listView_browse.Items.Count > 0)
                    this.listView_browse.Focus();
                else
                    this.textBox_queryWord.Focus();

                return 0;
            }
            finally
            {
                this._processing--;
            }
        ERROR1:
            try // ��ֹ����˳�ʱ����
            {
                this.textBox_resultInfo.Text = "���м�¼ " + this.listView_browse.Items.Count.ToString() + " ��";
                this.textBox_resultInfo.Text += "\r\n" + strError;

                MessageBox.Show(this, strError);

                this.textBox_queryWord.Focus();
            }
            catch
            {
            }
            return -1;
        }

        void EnableControls(bool bEnable)
        {
            this.textBox_queryWord.Enabled = bEnable;
            this.dtlpResDirControl1.Enabled = bEnable;
            // this.listView_browse.Enabled = bEnable;
            this.textBox_resPath.Enabled = bEnable;
            this.textBox_resultInfo.Enabled = bEnable;
        }

        // return:
        //      �����ظ��ļ�¼��
        int FillBrowseList(Package package,
            out string strError)
        {
            strError = "";

            int nDupCount = 0;

            // ����ÿ����¼
            for (int i = 0; i < package.Count; i++)
            {
                Application.DoEvents();	// ���ý������Ȩ

                if (stop != null)
                {
                    if (stop.State != 0)
                    {
                        strError = "�û��ж�";
                        return -1;
                    }
                }

                Cell cell = (Cell)package[i];

                // ����
                string strPath = DigitalPlatform.DTLP.Global.ModifyDtlpRecPath(cell.Path,
                    "");
                if (DetectDup(strPath) == true)
                {
                    nDupCount++;
                    continue;
                }

                ListViewItem item = new ListViewItem();
                item.Text = strPath;


                string[] cols = null;
                int nRet = GetOneBrowseRecord(
                    cell.Path,
                    out cols,
                    out strError);
                if (nRet == -1)
                {
                    item.SubItems.Add(strError);
                    goto CONTINUE;
                }
                if (cols != null)
                {
                    // ȷ���б��������㹻
                    ListViewUtil.EnsureColumns(this.listView_browse,
                        cols.Length,
                        200);
                    for (int j = 0; j < cols.Length; j++)
                    {
                        item.SubItems.Add(cols[j]);
                    }
                }

            CONTINUE:

                this.listView_browse.Items.Add(item);
                // this.listView_browse.UpdateItem(this.listView_browse.Items.Count - 1);
                
            }

            return nDupCount;
        }

        // ����Ƿ�����
        // ������ܣ���Hashtable������ٶ�
        // return:
        //      true    ����
        //      false   û����
        bool DetectDup(string strPath)
        {
            for (int i = 0; i < this.listView_browse.Items.Count; i++)
            {
                if (this.listView_browse.Items[i].Text == strPath)
                    return true;
            }

            return false;
        }

        private void DtlpSearchForm_Activated(object sender, EventArgs e)
        {
            if (stop != null)
                MainForm.stopManager.Active(this.stop);

            MainForm.SetMenuItemState();

            // �˵�
            if (this.listView_browse.SelectedItems.Count == 0)
            {
                MainForm.MenuItem_saveOriginRecordToIso2709.Enabled = false;
                MainForm.MenuItem_saveOriginRecordToWorksheet.Enabled = false;
            }
            else
            {
                MainForm.MenuItem_saveOriginRecordToIso2709.Enabled = true;
                MainForm.MenuItem_saveOriginRecordToWorksheet.Enabled = true;
            }

            MainForm.MenuItem_font.Enabled = false;



            // ��������ť
            if (this.listView_browse.SelectedItems.Count == 0)
            {
                MainForm.toolButton_saveTo.Enabled = false;
            }
            else
            {
                MainForm.toolButton_saveTo.Enabled = true;
            }
            MainForm.toolButton_save.Enabled = false;
            MainForm.toolButton_search.Enabled = true;
            MainForm.toolButton_prev.Enabled = false;
            MainForm.toolButton_next.Enabled = false;
            MainForm.toolButton_nextBatch.Enabled = false;

            MainForm.toolButton_getAllRecords.Enabled = false;

            MainForm.toolButton_delete.Enabled = false;
        }

        // ���յ������Ŀ����
        private void listView_browse_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            /*
            int nClickColumn = e.Column;

            // ����
            this.listView_browse.ListViewItemSorter = new ListViewBrowseItemComparer(nClickColumn);

            this.listView_browse.ListViewItemSorter = null;
             * */
            int nClickColumn = e.Column;

            ColumnSortStyle sortStyle = ColumnSortStyle.LeftAlign;

            // ��һ��Ϊ��¼·��������������
            if (nClickColumn == 0)
                sortStyle = ColumnSortStyle.RecPath;

            this.SortColumns.SetFirstColumn(nClickColumn,
                sortStyle,
                this.listView_browse.Columns,
                true);

            // ����
            this.listView_browse.ListViewItemSorter = new SortColumnsComparer(this.SortColumns);

            this.listView_browse.ListViewItemSorter = null;

        }
        // ��ʱȥ��?

        // �������˫�����
        private void listView_browse_DoubleClick(object sender, EventArgs e)
        {
            int nIndex = -1;
            if (this.listView_browse.SelectedIndices.Count > 0)
                nIndex = this.listView_browse.SelectedIndices[0];
            else
            {
                if (this.listView_browse.FocusedItem == null)
                    return;
                nIndex = this.listView_browse.Items.IndexOf(this.listView_browse.FocusedItem);
            }

            LoadDetail(nIndex);
        }

        void LoadDetail(int index)
        {
            MarcDetailForm form = new MarcDetailForm();

            form.MdiParent = this.MainForm;
            form.MainForm = this.MainForm;

            // MARC Syntax OID
            // ��Ҫ�������ݿ����ò��������еõ�MARC��ʽ
            ////form.AutoDetectedMarcSyntaxOID = "1.2.840.10003.5.1";   // UNIMARC

            form.Show();

            form.LoadRecord(this, index);
        }

        // �����¼
        public int SaveMarcRecord(
            string strPath,
            string strMARC,
            byte[] baTimestamp,
            out string strOutputPath,
            out byte[] baOutputTimestamp,
            out string strError)
        {
            strError = "";
            baOutputTimestamp = null;
            strOutputPath = "";

            int nRet = 0;

            if (baTimestamp == null)
                baTimestamp = new byte[9];

            // ���·������Ϊ׷�ӣ��������Ƿ�Ҫ��Ӧ���ã�

            int nWriteStyle = DtlpChannel.REPLACE_WRITE;

            // �ж�·���Ƿ�Ϊ׷��?
            {
                string strOutPath = "";
                // ���滯����·��
                // return:
                //      -1  error
                //      0   Ϊ���Ƿ�ʽ��·��
                //      1   Ϊ׷�ӷ�ʽ��·��
                nRet = DtlpChannel.CanonicalizeWritePath(strPath,
                    out strOutPath,
                    out strError);
                if (nRet == -1)
                    return -1;

                if (nRet == 1)
                    nWriteStyle = DtlpChannel.APPEND_WRITE;

                strPath = strOutPath;
            }

            string strOutputRecord = "";
            nRet = this.DtlpChannel.WriteMarcRecord(strPath,
                nWriteStyle,
                strMARC,
                baTimestamp,
                out strOutputRecord,
                out strOutputPath,
                out baOutputTimestamp,
                out strError);
            if (nRet == -1)
                return -1;


            return 0;
        }


        // �����¼
        public int DeleteMarcRecord(
            string strPath,
            byte[] baTimestamp,
            out string strError)
        {
            strError = "";
            int nRet = 0;

            if (baTimestamp == null)
                baTimestamp = new byte[9];

            nRet = this.DtlpChannel.DeleteMarcRecord(strPath,
                baTimestamp,
                out strError);
            if (nRet == -1)
                return -1;

            return 0;
        }
        
        protected override bool ProcessDialogKey(
            Keys keyData)
        {

            if (keyData == Keys.Enter)
            {
                if (this.textBox_queryWord.Focused == true)
                {
                    DoSearch();
                }
                else if (this.listView_browse.Focused == true)
                {
                    listView_browse_DoubleClick(this, null);
                }

                return true;
            }
            if (keyData == Keys.Escape)
            {
                MainForm.stopManager.DoStopActive();
                return true;
            }

            base.ProcessDialogKey(keyData);
            return false;
        }

        private void dtlpResDirControl1_ItemSelected(object sender, ItemSelectedEventArgs e)
        {
            if ((e.Mask & DtlpChannel.TypeStdbase) != 0
    || (e.Mask & DtlpChannel.TypeFrom) != 0)
                this.textBox_resPath.Text = e.Path;
            else
                this.textBox_resPath.Text = "";

        }

        private void dtlpResDirControl1_GetItemTextStyle(object sender, GetItemTextStyleEventArgs e)
        {
            e.FontFace = "";
            e.FontSize = 0;
            e.FontStyle = FontStyle.Regular;

            if ((e.Mask & DtlpChannel.TypeStdbase) != 0
                || (e.Mask & DtlpChannel.TypeFrom) != 0
                || (e.Mask & DtlpChannel.TypeKernel) != 0)
            {
                e.Result = 0;
            }
            else
            {
                e.ForeColor = ControlPaint.LightLight(ForeColor);
                e.Result = 1;
            }
        }

        /*
        protected override bool ProcessDialogChar(
            char charCode)
        {

            if (charCode == '\r')
            {
                if (this.textBox_queryWord.Focused == true)
                {
                    DoSearch();
                }
                else if (this.listView_browse.Focused == true)
                {
                    listView_browse_DoubleClick(this, null);
                }

                return true;
            }
            if (charCode == (char)((int)Keys.Escape))
            {
                MainForm.stopManager.DoStopActive();

                // this.DoStop(this, null);
                return true;
            }

            return false;
        }
         * */


#if NOOOOOOOOOOOOOOOOOO
                public void ItemSelected(string strPath, Int32 nMask)
        {
            if ((nMask & DtlpChannel.TypeStdbase) != 0
                || (nMask & DtlpChannel.TypeFrom) != 0)
                this.textBox_resPath.Text = strPath;
            else
                this.textBox_resPath.Text = "";

        }

        // �������Item���ֲ���
        public int ItemText(string strPath,
            Int32 nMask,
            out string strFontFace,
            out int nFontSize,
            out FontStyle FontStyle,
            ref Color ForeColor)
        {
            strFontFace = "";
            nFontSize = 0;
            FontStyle = FontStyle.Regular;

            if ((nMask & DtlpChannel.TypeStdbase) != 0
                || (nMask & DtlpChannel.TypeFrom) != 0)
            {

                /*
                strFontFace = "����";
                nFontSize = 12;
                FontStyle = FontStyle.Bold;
                ForeColor = Color.Red;
                */

                return 0;
            }
            else
            {
                ForeColor = ControlPaint.LightLight(ForeColor);
                return 1;
            }

        }
#endif
    }

    public class AccountItem
    {
        public string UserName = "public";
        public string Password = "";
        public bool SavePassword = false;
    }

    // Implements the manual sorting of items by columns.
    class ListViewBrowseItemComparer : IComparer
    {
        private int col = 0;
        public ListViewBrowseItemComparer()
        {
            col = 0;
        }
        public ListViewBrowseItemComparer(int column)
        {
            col = column;
        }
        public int Compare(object x, object y)
        {
            return String.Compare(((ListViewItem)x).SubItems[col].Text, ((ListViewItem)y).SubItems[col].Text);
        }
    }
}