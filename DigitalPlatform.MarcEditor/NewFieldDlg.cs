using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;

using DigitalPlatform.Xml;

namespace DigitalPlatform.Marc
{
    /// <summary>
    /// �����ֶεĶԻ���
    /// </summary>
    internal partial class NewFieldDlg : Form
    {
        public XmlDocument MarcDefDom = null;
        public string Lang = "zh";

        int nNested = 0;

        public NewFieldDlg()
        {
            InitializeComponent();
        }

        private void NewFieldDlg_Load(object sender, EventArgs e)
        {
            if (this.textBox_fieldName.Text == "")
                this.textBox_fieldName.Text = "???";

            this.AcceptButton = this.button_OK;

            string strError = "";
            int nRet = LoadFieldNames(out strError);
            if (nRet == -1)
                MessageBox.Show(this, strError);
        }

        int LoadFieldNames(out string strError)
        {
            strError = "";

            this.listView_fieldNameList.Items.Clear();

            if (this.MarcDefDom == null)
                return 0;

            XmlNodeList nodes = this.MarcDefDom.DocumentElement.SelectNodes("Field");

            for (int i = 0; i < nodes.Count; i++)
            {
                XmlNode node = nodes[i];

                string strName = DomUtil.GetAttr(node, "name");

                if (strName == "###")
                    continue;   // ����ͷ����

                string strLabel = "";

                XmlNode nodeProperty = node.SelectSingleNode("Property");
                if (nodeProperty != null)
                {
                    // ��һ��Ԫ�ص��¼��Ķ��<strElementName>Ԫ����, ��ȡ���Է��ϵ�XmlNode��InnerText
                    // parameters:
                    //      bReturnFirstNode    ����Ҳ���������Եģ��Ƿ񷵻ص�һ��<strElementName>
                    strLabel = DomUtil.GetXmlLangedNodeText(
                this.Lang,
                nodeProperty,
                "Label",
                true);
                }

#if NO
                XmlNode nodeLabel = null;
                try
                {
                    if (this.Lang == "")
                        nodeLabel = node.SelectSingleNode("Property/Label");
                    else
                    {
                        XmlNamespaceManager nsmgr = new XmlNamespaceManager(new NameTable());
                        nsmgr.AddNamespace("xml", Ns.xml);
                        nodeLabel = node.SelectSingleNode("Property/Label[@xml:lang='" + this.Lang + "']", nsmgr);
                    }
                }
                catch // ��ֹ�ֶ����в��Ϸ��ַ�����xpath�׳��쳣
                {
                    nodeLabel = null;
                }

                string strLabel = "";
                if (nodeLabel != null)
                {
                    strLabel = DomUtil.GetNodeText(nodeLabel);
                }
#endif

                ListViewItem item = new ListViewItem(strName);
                item.SubItems.Add(strLabel);

                this.listView_fieldNameList.Items.Add(item);
            }

            return 0;
        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            string strError = "";

            if (string.IsNullOrEmpty(this.textBox_fieldName.Text))
            {
                strError = "��δ�����ֶ���";
                goto ERROR1;
            }

            if (this.textBox_fieldName.Text.Length != 3)
            {
                strError = "�ֶ���ӦΪ3�ַ�";
                goto ERROR1;
            }

            if (this.textBox_fieldName.Text == "###")
            {
                strError = "������������Ϊ '###' ���ֶ�";
                goto ERROR1;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
            return;
        ERROR1:
            MessageBox.Show(this, strError);
        }

        private void button_Cancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        public string FieldName
        {
            get
            {
                return this.textBox_fieldName.Text;
            }
            set
            {
                this.textBox_fieldName.Text = value;
            }
        }

        public bool InsertBefore
        {
            get
            {
                if (this.radioButton_insertBefore.Checked == true)
                    return true;
                return false;
            }
            set
            {
                this.radioButton_insertBefore.Checked = value;
            }
        }


        /// <summary>
        /// ����Ի����
        /// </summary>
        /// <param name="keyData">System.Windows.Forms.Keys ֵ֮һ������ʾҪ����ļ���</param>
        /// <returns>����ؼ�����ʹ�û�������Ϊ true������Ϊ false���������һ������</returns>
        protected override bool ProcessDialogKey(
            Keys keyData)
        {
            // 2006/11/14 changed
            if (keyData == Keys.Enter)
            {
                button_OK_Click(null, null);
                return true;
            }

            if (keyData == Keys.Insert)
            {
                button_OK_Click(null, null);
                return true;
            }

            if (keyData == Keys.Escape)
            {
                button_Cancel_Click(null, null);
                return true;
            }


            return false;
        }

        private void listView_fieldNameList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (nNested > 0)
                return; // ��ֹû�б�Ҫ������

            if (this.listView_fieldNameList.SelectedItems.Count == 0)
                this.textBox_fieldName.Text = "";
            else
                this.textBox_fieldName.Text = this.listView_fieldNameList.SelectedItems[0].Text;

        }

        private void listView_fieldNameList_DoubleClick(object sender, EventArgs e)
        {
            button_OK_Click(null, null);
        }

        private void textBox_fieldName_KeyPress(object sender, KeyPressEventArgs e)
        {

        }

        private void textBox_fieldName_KeyUp(object sender, KeyEventArgs e)
        {
            // �Զ�����
            if (this.AutoComplete == true)
            {
                if (this.textBox_fieldName.SelectionStart >= 2
                    && this.textBox_fieldName.Text.Length == 3)
                {
                    button_OK_Click(null, null);
                }
            }

        }

        // �Ƿ����ֶ������뵽���һ���ַ�ʱ�Զ������Ի���
        public bool AutoComplete
        {
            get
            {
                return checkBox_autoComplete.Checked;
            }
            set
            {
                checkBox_autoComplete.Checked = value;
            }
        }

        // listview�е�ѡ����������ı��仯
        private void textBox_fieldName_TextChanged(object sender, EventArgs e)
        {
            if (nNested > 0)
                return; // ��ֹ��ѭ��

            nNested++;

            try
            {
                // bool bScrolled = false;
                int nStart = -1;
                int nEnd = -1;
                for (int i = 0; i < this.listView_fieldNameList.Items.Count; i++)
                {
                    string strText = this.listView_fieldNameList.Items[i].Text;

                    bool bHilight = false;

                    if (this.textBox_fieldName.Text.Length == 0)
                    {
                        bHilight = false;
                        goto CHANGE_COLOR;
                    }
                    else
                    {
                        if (this.textBox_fieldName.Text.Length < 3)
                        {
                            string strPart = strText.Substring(0, this.textBox_fieldName.Text.Length);

                            if (this.textBox_fieldName.Text == strPart)
                            {
                                bHilight = true;
                                goto CHANGE_COLOR;
                            }
                        }

                        if (this.textBox_fieldName.Text == strText)
                        {
                            bHilight = true;
                            goto CHANGE_COLOR;
                        }
                    }

                CHANGE_COLOR:
                    if (bHilight == false)
                    {
                        if (this.listView_fieldNameList.Items[i].BackColor != SystemColors.Window)
                            this.listView_fieldNameList.Items[i].BackColor = SystemColors.Window;
                        /*
                        if (this.listView_fieldNameList.Items[i].Selected != false)
                            this.listView_fieldNameList.Items[i].Selected = false;
                         * */
                    }
                    else
                    {
                        this.listView_fieldNameList.Items[i].BackColor = SystemColors.Info;

                        // this.listView_fieldNameList.Items[i].Selected = true;
                        /*
                        if (bScrolled == false)
                        {
                            this.listView_fieldNameList.EnsureVisible(i);
                            bScrolled = true;
                        }
                         * */

                        if (nStart == -1)
                            nStart = i;
                        nEnd = i;
                    }
                }

                if (nEnd != -1)
                    this.listView_fieldNameList.EnsureVisible(nEnd);
                if (nStart != -1)
                    this.listView_fieldNameList.EnsureVisible(nStart);

            }
            finally
            {
                nNested--;
            }
        }

    }
}