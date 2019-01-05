using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Xml;
using System.Diagnostics;
using System.Net;
using System.IO;
using System.Threading;
using System.Reflection;

using DigitalPlatform.Drawing;
using DigitalPlatform.Xml;
using DigitalPlatform.Text;

namespace DigitalPlatform.Marc
{

	public class MarcFixedFieldControl : System.Windows.Forms.ScrollableControl
	{
        public event GetTemplateDefEventHandler GetTemplateDef = null;  // �ⲿ�ӿڣ���ȡһ���ض�ģ���XML����
        public event EventHandler ResetSize = null;
        public event ResetTitleEventHandler ResetTitle = null;


		#region ��Ա����

        XmlNode nodeTemplateDef = null;

        List<XmlNode> CurValueListNodes = null;    // ��ǰ������ʾ��ֵ�б�ڵ�
        string CurActiveValueUnit = "";  // ��ǰ�Ѿ�������ֵ����

		public XmlDocument MarcDefDom = null;  // �����ļ���dom
		public string Lang = "";

		TemplateRoot templateRoot = null;
		private System.Windows.Forms.ListView listView_values = null;
		public int nCurLine = -1;

        Font m_fontDefaultInfo = null;
		// ���ֵ������ֺ�
        public Font DefaultInfoFont
        {
            get
            {
                if (this.m_fontDefaultInfo == null)
                    this.m_fontDefaultInfo = this.Font;

                return this.m_fontDefaultInfo;
            }
        }

        Font m_fontDefaultValue = null;
        private ImageList imageList_lineState;
        private IContainer components;
    
		public Font DefaultValueFont
        {
            get
            {
                if (this.m_fontDefaultValue == null)
                    this.m_fontDefaultValue = new Font("Courier New", this.Font.SizeInPoints, GraphicsUnit.Point);

                return this.m_fontDefaultValue;
            }
        }
            
            
            
        /*
        public Font DefaultValueFont
        {
            get
            {
                return this.Font;
            }
            set
            {
                this.Font = value;
            }
        }*/

		#endregion

        /// <summary>
        /// ������
        /// </summary>
        public event ParseMacroEventHandler ParseMacro = null;

        // public event GetConfigFileEventHandle GetConfigFile = null;

        /// <summary>
        /// ��������ļ��� XmlDocument ����
        /// </summary>
        public event GetConfigDomEventHandle GetConfigDom = null;

		public MarcFixedFieldControl()
		{
			// �õ����� Windows.Forms ���������������ġ�
			InitializeComponent();

			// TODO: �� InitComponent ���ú�����κγ�ʼ��
		}


		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if( components != null )
					components.Dispose();
			}
			base.Dispose( disposing );
		}

		#region �����������ɵĴ���
		/// <summary>
		/// �����֧������ķ��� - ��Ҫʹ�ô���༭�� 
		/// �޸Ĵ˷��������ݡ�
		/// </summary>
		private void InitializeComponent()
		{
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MarcFixedFieldControl));
            this.imageList_lineState = new System.Windows.Forms.ImageList(this.components);
            this.SuspendLayout();
            // 
            // imageList_lineState
            // 
            this.imageList_lineState.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList_lineState.ImageStream")));
            this.imageList_lineState.TransparentColor = System.Drawing.Color.White;
            this.imageList_lineState.Images.SetKeyName(0, "macro.bmp");
            this.imageList_lineState.Images.SetKeyName(1, "sensitive.bmp");
            this.imageList_lineState.Images.SetKeyName(2, "macro_and_sensitive.bmp");
            // 
            // MarcFixedFieldControl
            // 
            this.Name = "MarcFixedFieldControl";
            this.Size = new System.Drawing.Size(168, 160);
            this.Enter += new System.EventHandler(this.MarcFixedFieldControl_Enter);
            this.ResumeLayout(false);

		}
		#endregion

		#region ����

#if NO
		// ���һ��ref�ַ���Ϊ��Դ��ValueList������
		// parameters:
		//		strRef	ref�ַ���
		//		strFirst5	������Դʱ����Դ��ǰ5���ַ�
		//		strSource	��Դ
		//		strValueListName	ValueList������
		//		strError	������Ϣ
		// return:
		//		-1	ʧ��
		//		0	�ɹ�
		// ע: ��Դ��ValueList����֮����#�ָ�
		public int SplitRef(string strRef,
			out string strFirst5,
			out string strSource,
			out string strValueListName,
			out string strError)
		{
			strError = "";

			strFirst5 = "";
			strSource = "";
			strValueListName = "";
			

			int nIndex = strRef.LastIndexOf('#');
			if (nIndex == -1)
			{
				if (strRef.Length < 5)
				{
					strFirst5 = "";
					strSource = "";
					strValueListName = strRef;
					return 0;
				}
				else
				{
					strFirst5 = strRef.Substring(0,5);
					if (strFirst5 == "http:"
						|| strFirst5 == "file:")
					{
						strSource = strRef;
						strValueListName = "";
						return 0;
					}
					else
					{
						strSource = "";
						strValueListName = strRef;
						return 0;
					}
				}
			}

			strSource = strRef.Substring(0,nIndex);
			strValueListName = strRef.Substring(nIndex + 1);
			if (strSource.Length < 5)
			{
                strError = "��Դ'" + strSource + "'���Ϸ���ǰ5���ַ�������'http:'��'file:'";
				return -1;					
			}
			else
			{
				strFirst5 = strSource.Substring(0,5);
				if (strFirst5 != "http:"
					&& strFirst5 != "file:")
				{
					strError = "��Դ'" + strRef + "'���Ϸ���ǰ5���ַ�������'http:'��'file:'";
					return -1;					
				}
			}

			if (strSource == ""
				&& strFirst5 != "")
			{
				strError = "ref��Դ'" + strRef + "'���Ϸ�";
				return -1;
			}
			return 0;
		}
#endif

		// �õ�Label�������
		public void GetMaxWidth(//Graphics g,
            out int nMaxLabelWidth,
            out int nMaxNameWidth)
		{
			nMaxLabelWidth = 0;
            nMaxNameWidth = 0;
			for(int i=0;i<this.templateRoot.Lines.Count;i++)
			{
				TemplateLine line = (TemplateLine)this.templateRoot.Lines[i];

                string strTemp = line.m_strLabel;
                // strTemp = strTemp.Replace(" ", "M");    // +"M";   // 2008/7/16
                int nOneLabelWidth = GraphicsUtil.GetWidth(//g,
                    this.DefaultInfoFont,
                    strTemp /*line.m_strLabel*/);   // +strTemp.Length * 2;

				if (nOneLabelWidth > nMaxLabelWidth)
					nMaxLabelWidth = nOneLabelWidth;

                // 
                int nOneNameWidth = GraphicsUtil.GetWidth(//g,
                    this.DefaultInfoFont,
                    line.m_strName);

                if (nOneNameWidth > nMaxNameWidth)
                    nMaxNameWidth = nOneNameWidth;
			}
		}


		// ��ʼ���ؼ�
		// paramters:
		//		fieldNode	Field�ڵ�
		//		strLang	���԰汾
		// return:
		//		-1	����
		//		0	���Ƕ����ֶ�
		//		1	�ɹ�
		public int Initial(XmlNode node,
			string strLang,
            out int nResultWidth,
            out int nResultHeight,
			out string strError)
		{
            nResultWidth = 0;
            nResultHeight = 0;
			
			strError = "";

			this.Lang = strLang;

            this.Controls.Clear();

			this.templateRoot = new TemplateRoot(this);
			int nRet = this.templateRoot.Initial(node,
				strLang,
				out strError);
			if (nRet == 0)
				return 0;
			if (nRet == -1)
				return -1;

            for (int i = 0; i < this.templateRoot.Lines.Count; i++)
            {
                TemplateLine line = (TemplateLine)this.templateRoot.Lines[i];

                /*
                string strTempValue = line.m_strValue;
                strTempValue = strTempValue.Replace(' ', 'M');   // ?

                nValueWidth = GraphicsUtil.GetWidth(//g,
                    this.DefaultValueFont,
                    strTempValue);

                if (nMaxValueWidth < nValueWidth)
                    nMaxValueWidth = nValueWidth;

                int nHeight = this.DefaultValueFont.Height;
                 * */

                ValueEditBox textBox_value = new ValueEditBox();
                textBox_value.Font = this.DefaultValueFont;
                textBox_value.Name = "value_" + Convert.ToString(i);
                textBox_value.TabIndex = i;
                textBox_value.Text = line.m_strValue;
                textBox_value.MaxLength = line.m_nValueLength;
                textBox_value.nIndex = i;
                textBox_value.fixedFieldCtrl = this;
                this.Controls.Add(textBox_value);
                line.TextBox_value = textBox_value;
                textBox_value.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
                textBox_value.ForeColor = SystemColors.WindowText;
                textBox_value.BackColor = SystemColors.Window;
                textBox_value.TextChanged -= new EventHandler(textBox_value_TextChanged);
                textBox_value.TextChanged += new EventHandler(textBox_value_TextChanged);

                Label label_label = new Label();
                label_label.Font = this.DefaultInfoFont;
                label_label.Name = "label_" + Convert.ToString(i);
                label_label.TabIndex = i;
                label_label.Text = line.m_strLabel;
                label_label.TextAlign = ContentAlignment.MiddleRight;
                // label_label.BorderStyle = BorderStyle.FixedSingle;
                this.Controls.Add(label_label);
                line.Label_label = label_label;

                Label label_name = new Label();
                label_name.Font = this.DefaultInfoFont;
                label_name.Name = "name_" + Convert.ToString(i);
                label_name.TabIndex = i;
                label_name.Text = line.m_strName;
                // label_name.BorderStyle = BorderStyle.Fixed3D;
                label_name.BorderStyle = BorderStyle.None;
                // label_name.BackColor = SystemColors.Window;
                label_name.ForeColor = SystemColors.GrayText;
                this.Controls.Add(label_name);
                line.Label_Name = label_name;

                Label label_state = new Label();
                label_state.Font = this.DefaultInfoFont;
                label_state.Name = "state_" + Convert.ToString(i);
                label_state.TabIndex = i;
                label_state.Text = "";
                label_state.BorderStyle = BorderStyle.None;
                label_state.ImageList = this.imageList_lineState;
                this.Controls.Add(label_state);
                line.Label_state = label_state;
                line.LineState = line.LineState;
            }

            this.listView_values = new ListView();
            this.listView_values.Name = "listView_values";
            this.listView_values.TabIndex = 100;
            this.listView_values.View = View.Details;
            this.listView_values.Columns.Add("ֵ", 50, HorizontalAlignment.Left);
            this.listView_values.Columns.Add("˵��", 700, HorizontalAlignment.Left);
            this.listView_values.FullRowSelect = true;
            this.listView_values.HideSelection = false;
            this.AutoScroll = true;
            this.listView_values.DoubleClick += new System.EventHandler(this.ValueList_DoubleClick);
            // this.listView_values.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
            this.listView_values.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            this.listView_values.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Controls.Add(this.listView_values);

#if NO
            this.SuspendLayout();


			// Graphics g = Graphics.FromHwnd(this.Handle);

			int nJianGeWidth = 8;
			// int nNameWidth = 60;
            int nNameWidth = GraphicsUtil.GetWidth(this.DefaultInfoFont,
					"99/99");

            int nLabelWidth = this.GetMaxLabelWidth(//g,
                this.DefaultInfoFont);  // +8;
			int nValueWidth = 0;

			int nMaxValueWidth = 0;
			int x = 0;
			int y = 0;
			for(int i=0;i<this.templateRoot.Lines.Count;i++)
			{
				TemplateLine line = (TemplateLine)this.templateRoot.Lines[i];

				string strTempValue = line.m_strValue;
				// strTempValue = strTempValue.Replace(' ','m');   // ?
                /*
                string strTemp = "";
                strTempValue = strTemp.PadLeft(strTempValue.Length+1, 'M');   // 2008/7/16
                 * */
                strTempValue = strTempValue.Replace(' ','M');   // ?

				nValueWidth = GraphicsUtil.GetWidth(//g,
					this.DefaultValueFont,
					strTempValue);

				if (nMaxValueWidth < nValueWidth)
					nMaxValueWidth = nValueWidth;

				int nHeight = this.DefaultValueFont.Height;

				ValueEditBox textBox_value = new ValueEditBox();

                // textBox_value.BorderStyle = BorderStyle.None;
                // ((TextBoxBase)textBox_value).AutoSize = false;
				textBox_value.Font = this.DefaultValueFont;
				textBox_value.Location = new System.Drawing.Point(x + nLabelWidth + nJianGeWidth + nNameWidth + nJianGeWidth, 0 + y);
				textBox_value.Name = "value_" + Convert.ToString(i);
				textBox_value.TabIndex = i;
				textBox_value.Text = line.m_strValue;
				textBox_value.MaxLength = line.m_nValueLength;
				textBox_value.nIndex = i;
				textBox_value.fixedFieldCtrl = this;
				this.Controls.Add(textBox_value);
				line.TextBox_value = textBox_value;
				textBox_value.ClientSize = new System.Drawing.Size(nValueWidth, 
                    textBox_value.ClientSize.Height);
                    // nHeight); // changed
                textBox_value.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
                textBox_value.ForeColor = SystemColors.WindowText;
                textBox_value.BackColor = SystemColors.Window;
                textBox_value.TextChanged -= new EventHandler(textBox_value_TextChanged);
                textBox_value.TextChanged += new EventHandler(textBox_value_TextChanged);

				nHeight = textBox_value.Size.Height;

				Label label_label = new Label();
				label_label.Font = this.DefaultInfoFont;
				label_label.Location = new System.Drawing.Point(x + 0, 0 + y);
				label_label.Size = new Size(nLabelWidth,nHeight);
				label_label.Name = "label_" + Convert.ToString(i);
				label_label.TabIndex = i;
				label_label.Text = line.m_strLabel;
				label_label.TextAlign = ContentAlignment.MiddleRight;
				// label_label.BorderStyle = BorderStyle.FixedSingle;
				this.Controls.Add(label_label);
				line.Label_label = label_label;

				Label label_name = new Label();
				label_name.Font = this.DefaultInfoFont;
				label_name.Location = new System.Drawing.Point(x + nLabelWidth + nJianGeWidth, 0 + y);
				label_name.Size = new Size(nNameWidth,nHeight);
				label_name.Name = "name_" + Convert.ToString(i);
				label_name.TabIndex = i;
				label_name.Text = line.m_strName;
				// label_name.BorderStyle = BorderStyle.Fixed3D;
                label_name.BorderStyle = BorderStyle.None;
                label_name.BackColor = SystemColors.Window;
                label_name.ForeColor = SystemColors.GrayText;
                this.Controls.Add(label_name);
				line.Label_Name = label_name;

			

				y = y + nHeight;
			}

			int nListViewOrgX = nLabelWidth + nJianGeWidth + nNameWidth + nJianGeWidth + nMaxValueWidth + 10;
			int nListViewOrgY = 0;

			int nListViewWidth = 200;
			int nListViewHeight = Math.Max(y, 10 * this.DefaultValueFont.Height);  // ��֤�߶Ȳ���̫С

			this.listView_values = new ListView();
			this.listView_values.Location = new System.Drawing.Point(nListViewOrgX,nListViewOrgY);
			this.listView_values.Name = "listView_values";
			this.listView_values.TabIndex = 100;
			//this.listView_values.Size = new Size(nListViewWidth,nListViewHeight);
            this.listView_values.Size = new Size(nListViewWidth, nListViewHeight);

            this.listView_values.MinimumSize = new Size(nListViewWidth/2, nListViewHeight);
            this.listView_values.MaximumSize = new Size(1024, 768);


			this.listView_values.View = View.Details;
			this.listView_values.Columns.Add("ֵ", 50, HorizontalAlignment.Left);
			this.listView_values.Columns.Add("˵��",700, HorizontalAlignment.Left);
			this.listView_values.FullRowSelect = true;
			this.listView_values.HideSelection = false;
            this.AutoScroll = true;
			this.listView_values.DoubleClick += new System.EventHandler(this.ValueList_DoubleClick);
            this.listView_values.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;

			this.Controls.Add(this.listView_values);



			int nDocumentWith = nListViewOrgX + nListViewWidth;
            int nDocumentHeight = nListViewHeight;

			//this.Size = new Size(nDocumentWith,
			//	nDocumentHeight);
            nResultWidth = nDocumentWith + 100;
            nResultHeight = nDocumentHeight;   // xietao change

            this.ResumeLayout(false);
            this.PerformLayout();
#endif
            this.nodeTemplateDef = node;    // ���涨��ڵ�

            AdjustTextboxSize(
                true,
             out nResultWidth,
             out nResultHeight);

            return 1;
		}

        internal int m_nDisableTextChanged = 0;

        void textBox_value_TextChanged(object sender, EventArgs e)
        {
            if (this.m_nDisableTextChanged > 0)
                return;

            m_nDisableTextChanged++;
            try
            {
                string strError = "";

                TextBox textbox = (TextBox)sender;

                if (string.IsNullOrEmpty(textbox.Text) == true)
                    return;

                TemplateLine changed_line = null;
                foreach (TemplateLine line in this.templateRoot.Lines)
                {
                    if (line.TextBox_value == textbox)
                    {
                        changed_line = line;
                        goto FOUND;
                    }
                }
                return;
            FOUND:
                Debug.Assert(changed_line != null, "");
                if (changed_line.IsSensitive == true
                    && this.GetTemplateDef != null)
                {
                    // ��Ҫ����װ��ģ�嶨��
                    GetTemplateDefEventArgs e1 = new GetTemplateDefEventArgs();
                    if (this.nodeTemplateDef != null)
                    {
                        if (this.nodeTemplateDef.Name == "Field")
                        {
                            e1.FieldName = DomUtil.GetAttr(this.nodeTemplateDef, "name");
                        }
                        else if (this.nodeTemplateDef.Name == "Subfield")
                        {
                            e1.FieldName = DomUtil.GetAttr(this.nodeTemplateDef.ParentNode, "name");
                            e1.SubfieldName = DomUtil.GetAttr(this.nodeTemplateDef, "name");
                        }
                        else
                        {
                            strError = "ģ�嶨��ڵ�Ϊ����� <" + this.nodeTemplateDef.Name + "> Ԫ�أ��޷��������д���";
                            goto ERROR1;
                        }
                    }
                    else
                    {
                        strError = "��ǰû��ģ�嶨��ڵ���Ϣ���޷��������д���";
                        goto ERROR1;
                    }

                    string strValue = this.Value + this.AdditionalValue;
                    e1.Value = strValue;

                    this.GetTemplateDef(this, e1);

                    if (string.IsNullOrEmpty(e1.ErrorInfo) == false)
                    {
                        strError = "���ģ�嶨��ʱ����: " + e1.ErrorInfo;
                        goto ERROR1;
                    }

                    if (e1.Canceled == true)
                        return;

                    if (e1.DefNode == this.nodeTemplateDef)
                        return; // ģ�嶨��û�з����仯

                    int nResultWidth = 0;
                    int nResultHeight = 0;

                    // paramters:
                    //		fieldNode	Field�ڵ�
                    //		strLang	���԰汾
                    // return:
                    //		-1	����
                    //		0	���Ƕ����ֶ�
                    //		1	�ɹ�
                    int nRet = this.Initial(e1.DefNode,
                this.Lang,
                out nResultWidth,
                out nResultHeight,
                out strError);
                    if (nRet != 1)
                        goto ERROR1;

                    this.Value = strValue;

                    // ֪ͨForm�ߴ緢���˱仯
                    if (this.ResetSize != null)
                    {
                        this.ResetSize(this, new EventArgs());
                    }

                    // ֪ͨ����仯
                    if (this.ResetTitle != null)
                    {
                        ResetTitleEventArgs e2 = new ResetTitleEventArgs();
                        e2.Title = e1.Title;
                        this.ResetTitle(this, e2);
                    }
                }
                return;
            ERROR1:
                MessageBox.Show(this, strError);
            }
            finally
            {
                m_nDisableTextChanged--;
            }
        }

        TemplateLine GetLine(ValueEditBox textbox)
        {
            foreach (TemplateLine line in this.templateRoot.Lines)
            {
                if (line.TextBox_value == textbox)
                {
                    return line;
                }
            }

            return null;
        }

        // ���ȱʡֵ
        // return:
        //      -1  error
        //      0   not found
        //      1   found
        public int GetDefaultValue(int index,
            out string strValue,
            out string strError)
        {
            strValue = "";
            strError = "";

            TemplateLine line = (TemplateLine)this.templateRoot.Lines[index];
			if (line == null)
			{
                strError = "δ�ҵ����Ϊ'" + Convert.ToString(index) + "'��";
                return -1;
			}

            if (line.DefaultValue == null)
            {
                strError = "ȱʡֵδ����";
                return 0;
            }

            if (this.ParseMacro == null)
            {
                strError = "û�йҽ��¼�";
                return 0;    // û�йҽ��¼�
            }

            ParseMacroEventArgs e = new ParseMacroEventArgs();
            e.Macro = line.DefaultValue;
            this.ParseMacro(this, e);
            if (String.IsNullOrEmpty(e.ErrorInfo) == false)
            {
                strError = e.ErrorInfo;
                return -1;
            }

            strValue = e.Value;
            return 1;
        }

        public void BeginShowValueList(
            int nCurLine,
            int nCaretPosition,
            int nLineChars,
            string strCurLine)
        {
            this.nCurLine = nCurLine;

            object[] pList = { nCaretPosition, nLineChars, strCurLine };

            this.BeginInvoke(new Delegate_ShowValueList(ShowValueList),
                pList);
        }

        public delegate void Delegate_ShowValueList(
            int nCaretPosition,
            int nLineChars,
            string strCurLine);

        int m_nInShowValueList = 0;

		// ��ʾֵ�б�
		public void ShowValueList(
            int nCaretPosition,
            int nLineChars,
            string strCurLine)
		{
			this.Update();

			if (this.nCurLine == -1)
				return;

            if (this.m_nInShowValueList > 0)
            {
                // TODO: �Ƿ�Postһ����Ϣ��ȥ���Ժ�����?
                return;
            }


            this.m_nInShowValueList++;
            try
            {
                string strError = "";

                TemplateLine line = (TemplateLine)this.templateRoot.Lines[this.nCurLine];
                if (line == null)
                {
                    strError = "δ�ҵ����Ϊ'" + Convert.ToString(this.nCurLine) + "'��";
                    return;
                }

                int nValueChars = 0;    // ֵ���ַ���

                if (line.ValueListNodes == null
                    || line.ValueListNodes.Count == 0)
                {
                    this.listView_values.Items.Clear();
                    return;
                }

                Cursor oldCursor = this.Cursor;
                this.Cursor = Cursors.WaitCursor;

                if (this.CurValueListNodes != null
                    && this.CurValueListNodes == line.ValueListNodes)
                {
                    if (this.listView_values.Items.Count > 0)
                    {
                        nValueChars = this.listView_values.Items[0].Text.Length;
                        goto SELECT; // �Ż�
                    }
                }

                this.listView_values.Items.Clear();

                // ���ref
                while (true)
                {
                    bool bFoundNew = false;
                    for (int i = 0; i < line.ValueListNodes.Count; i++)
                    {
                        XmlNode valuelist_node = line.ValueListNodes[i];
                        string strRef = DomUtil.GetAttr(valuelist_node, "ref");
                        if (string.IsNullOrEmpty(strRef) == true)
                            continue;

                        {
                            if (this.GetConfigDom == null)
                                return;

                            GetConfigDomEventArgs ar = new GetConfigDomEventArgs();
                            ar.Path = strRef;
                            ar.XmlDocument = null;
                            this.GetConfigDom(this, ar);
                            if (string.IsNullOrEmpty(ar.ErrorInfo) == false)
                            {
                                strError = "��ȡ '" + line.m_strName + "' ��Ӧ��ValueList����ԭ��:" + ar.ErrorInfo;
                                goto END1;
                            }
                            if (ar.XmlDocument == null)
                                return; // ??

                            string strSource = "";
                            string strValueListName = "";
                            int nIndex = strRef.IndexOf('#');
                            if (nIndex != -1)
                            {
                                strSource = strRef.Substring(0, nIndex);
                                strValueListName = strRef.Substring(nIndex + 1);
                            }
                            else
                            {
                                strValueListName = strRef;
                            }
                            XmlNode valueListNode = ar.XmlDocument.SelectSingleNode("//ValueList[@name='" + strValueListName + "']");
                            if (valueListNode == null)
                            {
                                strError = "δ�ҵ�·��Ϊ'" + strRef + "'�Ľڵ㡣";
                                goto END1;
                            }

                            // �滻
                            line.ValueListNodes[i] = valueListNode;
                            bFoundNew = true;
                        }
                    }

                    if (bFoundNew == false)
                        break;
                }

                this.listView_values.Items.Clear(); // 2006/5/30 add

                foreach (XmlNode valuelist_node in line.ValueListNodes)
                {
                    XmlNodeList itemList = valuelist_node.SelectNodes("Item");
                    foreach (XmlNode itemNode in itemList)
                    {
                        string strItemLable = "";

                        // ��һ��Ԫ�ص��¼��Ķ��<strElementName>Ԫ����, ��ȡ���Է��ϵ�XmlNode��InnerText
                        // parameters:
                        //      bReturnFirstNode    ����Ҳ���������Եģ��Ƿ񷵻ص�һ��<strElementName>
                        strItemLable = DomUtil.GetXmlLangedNodeText(
                    this.Lang,
                    itemNode,
                    "Label",
                    true);
                        if (string.IsNullOrEmpty(strItemLable) == true)
                            strItemLable = "<��δ����>";
                        else
                            strItemLable = StringUtil.Trim(strItemLable);

#if NO
                        XmlNamespaceManager nsmgr = new XmlNamespaceManager(new NameTable());
                        nsmgr.AddNamespace("xml", Ns.xml);
                        XmlNode itemLabelNode = itemNode.SelectSingleNode("Label[@xml:lang='" + this.Lang + "']", nsmgr);
                        if (itemLabelNode == null
                            || string.IsNullOrEmpty(itemLabelNode.InnerText.Trim()) == true)
                        {
                            // ����Ҳ��������ҵ���һ����ֵ��
                            XmlNodeList nodes = itemNode.SelectNodes("Label", nsmgr);
                            foreach (XmlNode temp_node in nodes)
                            {
                                if (string.IsNullOrEmpty(temp_node.InnerText.Trim()) == false)
                                {
                                    itemLabelNode = temp_node;
                                    break;
                                }
                            }
                        }

                        if (itemLabelNode == null)
                            strItemLable = "<��δ����>";
                        else
                            strItemLable = StringUtil.Trim(itemLabelNode.InnerText);
#endif


                        XmlNode itemValueNode = itemNode.SelectSingleNode("Value");
                        string strItemValue = StringUtil.Trim(itemValueNode.InnerText);

                        if (String.IsNullOrEmpty(strItemValue) == false)
                        {
                            nValueChars = strItemValue.Length;
                        }

                        // ����listview
                        ListViewItem item = new ListViewItem(strItemValue);
                        item.SubItems.Add(strItemLable);
                        this.listView_values.Items.Add(item);
                    }
                }

                this.CurValueListNodes = line.ValueListNodes;

            SELECT:

                // ������ǰ����
                if (nValueChars != 0)
                {
                    // �����ǰ������ڵڼ�����Ԫ��ֵ��
                    int nIndex = nCaretPosition / nValueChars;
                    if ((nIndex * nValueChars) + nValueChars <= strCurLine.Length)
                    {

                        string strCurUnit = strCurLine.Substring(nIndex * nValueChars, nValueChars);

                        // ��listview���ҵ����ֵ, ��ѡ������
                        for (int i = 0; i < this.listView_values.Items.Count; i++)
                        {
                            if (this.listView_values.Items[i].Text.Replace("_", " ") == strCurUnit)
                            {
                                this.CurActiveValueUnit = strCurUnit;

                                this.listView_values.Items[i].Selected = true;
                                // ����������
                                this.listView_values.EnsureVisible(i);
                            }
                            else
                            {
                                if (this.listView_values.Items[i].Selected != false)
                                    this.listView_values.Items[i].Selected = false;
                            }
                        }
                    }

                }

            END1:
                /////////////////////////////////////
                // ����EndGetValueList�¼�
                ///////////////////////////////////////
                EndGetValueListEventArgs argsEnd = new EndGetValueListEventArgs();
                if (strError != "")
                    argsEnd.Ref = strError;
                else
                    argsEnd.Ref = "";
                this.fireEndGetValueList(this, argsEnd);
                this.Cursor = oldCursor;
            }
            finally
            {
                this.m_nInShowValueList--;
            }
		}

        public int LineCount
        {
            get
            {
                return this.templateRoot.Lines.Count;
            }
        }

        ValueEditBox m_currentTextBox = null;   // ��ǰ���ڽ����TextBox

        public void ChangeFocusDisplay(ValueEditBox textbox)
        {
            if (this.m_currentTextBox != null)
            {
                this.m_currentTextBox.Font = this.DefaultValueFont;
                this.m_currentTextBox.ForeColor = SystemColors.WindowText;
                this.m_currentTextBox.BackColor = SystemColors.Window;
            }

            this.m_currentTextBox = textbox;
            this.m_currentTextBox.Font = new Font(this.DefaultValueFont, FontStyle.Bold);
            this.m_currentTextBox.BackColor = Color.LightYellow;
            this.m_currentTextBox.ForeColor = Color.DarkRed;
        }

        public ValueEditBox SwitchFocus(int nLineIndex,
            CaretPosition caretposition)
        {
            if (nLineIndex >= templateRoot.Lines.Count)
                return null; // �±�Խ��

            TemplateLine line = (TemplateLine)templateRoot.Lines[nLineIndex];

            ChangeFocusDisplay(line.TextBox_value);

            line.TextBox_value.Focus();
            // this.ScrollToControl(line.TextBox_value);


            if (caretposition == CaretPosition.FirstChar)
            {
                line.TextBox_value.SelectionStart = 0;
                line.TextBox_value.SelectionLength = 0;
            }

            if (caretposition == CaretPosition.LastChar)
            {
                line.TextBox_value.SelectionStart = line.TextBox_value.MaxLength - 1;
                line.TextBox_value.SelectionLength = 0;
            }

            return line.TextBox_value;
        }


        /*
�������� crashReport -- �쳣���� 
���� dp2catalog 
������ xxx
ý������ text 
���� ����δ����Ľ����߳��쳣: 
Type: System.ArgumentOutOfRangeException
Message: InvalidArgument=��0����ֵ���ڡ�index����Ч��
������: index
Stack:
�� System.Windows.Forms.ListView.SelectedListViewItemCollection.get_Item(Int32 index)
�� DigitalPlatform.Marc.MarcFixedFieldControl.ValueList_DoubleClick(Object sender, EventArgs e)
�� System.Windows.Forms.Control.OnDoubleClick(EventArgs e)
�� System.Windows.Forms.ListView.WndProc(Message& m)
�� System.Windows.Forms.Control.ControlNativeWindow.OnMessage(Message& m)
�� System.Windows.Forms.Control.ControlNativeWindow.WndProc(Message& m)
�� System.Windows.Forms.NativeWindow.Callback(IntPtr hWnd, Int32 msg, IntPtr wparam, IntPtr lparam)


dp2Catalog �汾: dp2Catalog, Version=2.4.5714.24078, Culture=neutral, PublicKeyToken=null
����ϵͳ��Microsoft Windows NT 5.1.2600 Service Pack 3 
����ʱ�� 2015/9/17 16:38:54 (Thu, 17 Sep 2015 16:38:54 +0800) 
ǰ�˵�ַ xxx ���� http://dp2003.com/dp2library 

         * */
        // ����ValueList�б���˫��ĳ��ʱ��ֵ�ص���Ӧ�ĵط�
		private void ValueList_DoubleClick(object sender, System.EventArgs e)
		{
            string strError = "";

            if (this.listView_values.SelectedItems.Count == 0)
            {
                strError = "��δѡ������";
                goto ERROR1;
            }

			ListViewItem item = this.listView_values.SelectedItems[0];

            // �����ļ��п���ʹ�� '_' ����ո�
			string strValue = item.Text.Trim().Replace("_", " ");
			//MessageBox.Show(this,strValue);

            if (string.IsNullOrEmpty(strValue) == true)
            {
                strError = "ֵΪ�ա��붨��һ��ֵ�ַ���";
                goto ERROR1;
            }

			TemplateLine line = (TemplateLine)this.templateRoot.Lines[this.nCurLine];
			int nPosition = line.TextBox_value.SelectionStart;

			if (nPosition == line.m_nValueLength)
				nPosition = line.m_nValueLength -1;

			// �õ���ȷ�Ĳ����λ��
			if (line.TextBox_value.MaxLength % strValue.Length != 0)
			{
                strError = "ֵ�ַ�������������ֵ��������";  // 2009/9/21 add
				goto ERROR1;
			}

			string strTempValue = line.m_strValue;
			if (strTempValue.Length < line.m_nValueLength)
				strTempValue = strTempValue + new string(' ',line.m_nValueLength - strTempValue.Length);

			nPosition = (nPosition/strValue.Length)*strValue.Length;

			strTempValue = strTempValue.Remove(nPosition,strValue.Length);
			strTempValue = strTempValue.Insert(nPosition,strValue);

			line.m_strValue = strTempValue;
			line.TextBox_value.Text = strTempValue;
			line.TextBox_value.SelectionStart = nPosition + strValue.Length;

			line.TextBox_value.Focus();
            line.TextBox_value.NextLineIfNeed();
            return;
        ERROR1:
            MessageBox.Show(this, strError);
		}
		
		// �õ��ֶε�ֵ
		public string Value
		{
			get
			{
                if (this.templateRoot != null)
				    return this.templateRoot.GetValue();

                return "";
			}
			set
			{
                if (templateRoot != null)
                {
                    m_nDisableTextChanged++;
                    this.templateRoot.SetValue(value);
                    m_nDisableTextChanged--;
                }
			}
		}

        // �ദ�����ַ�������
        public string AdditionalValue
        {
            get
            {
                if (this.templateRoot != null)
                    return this.templateRoot.AdditionalValue;

                return "";
            }
            set
            {
                if (templateRoot != null)
                    this.templateRoot.AdditionalValue = value;
            }
        }

		#endregion

		#region �¼�

		public event BeginGetValueListEventHandle BeginGetValueList;
		public void fireBeginGetValueList(object sender,
			BeginGetValueListEventArgs args)
		{
			if (BeginGetValueList != null)
			{
				this.BeginGetValueList(sender,args);
			}
		}

		public event EndGetValueListEventHandle EndGetValueList;
		public void fireEndGetValueList(object sender,
			EndGetValueListEventArgs args)
		{
			if (EndGetValueList != null)
			{
				this.EndGetValueList(sender,args);
			}
		}

		#endregion

        private void MarcFixedFieldControl_Enter(object sender, EventArgs e)
        {
            if (this.templateRoot.Lines.Count > 0)
            {

                TemplateLine line = null;
                if (this.m_currentTextBox != null)
                    line = GetLine(this.m_currentTextBox);
                else 
                    line = (TemplateLine)this.templateRoot.Lines[0];

                if (line == null)
                    return;

                ChangeFocusDisplay(line.TextBox_value);
                line.TextBox_value.Focus();
            }
        }

        public void FocusFirstLine()
        {
            // �������Ͼ�����Ա�Ϊ��һ�ξ�����Խ�����ĳ������
            if (this.templateRoot.Lines.Count > 0)
            {
                TemplateLine line = (TemplateLine)this.templateRoot.Lines[0];
                line.TextBox_value.SelectionStart = 0;
                line.TextBox_value.SelectionLength = 0;

                ChangeFocusDisplay(line.TextBox_value);
                line.TextBox_value.Focus();
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            AdjuetListViewSize();
        }

        public void AdjuetListViewSize()
        {
            if (listView_values != null)
            {
                int nWidth = this.Width - this.listView_values.Location.X;
                this.listView_values.Size = new Size(nWidth, this.Size.Height);
            }
        }

        public void AdjustTextboxSize(
            bool bSimulate,
            out int nResultWidth,
            out int nResultHeight
            )
        {
            this.SuspendLayout();

            int nLabelWidth = 0;
            int nNameWidth = 0;
            GetMaxWidth(//Graphics g,
  out nLabelWidth,
  out nNameWidth);

            int y = this.Padding.Top;
            int nBlankWidth = 8;
            int nLineSep = 4;
            int nStateWidth = 8;

            int nMaxValueWidth = 0;
            for (int i = 0; i < this.templateRoot.Lines.Count; i++)
            {
                if (i != 0)
                    y += nLineSep;

                TemplateLine line = (TemplateLine)this.templateRoot.Lines[i];

                string strTempValue = line.m_strValue;
                // strTempValue = strTempValue.Replace(' ','m');   // ?
                /*
                string strTemp = "";
                strTempValue = strTemp.PadLeft(strTempValue.Length+1, 'M');   // 2008/7/16
                 * */
                strTempValue = strTempValue.Replace(' ', 'M');   // ?

                int nValueWidth = GraphicsUtil.GetWidth(//g,
                    this.DefaultValueFont,
                    strTempValue);

                if (nMaxValueWidth < nValueWidth)
                    nMaxValueWidth = nValueWidth + line.TextBox_value.Size.Width - line.TextBox_value.ClientSize.Width;

                if (bSimulate == false)
                {
                    line.TextBox_value.ClientSize = new Size(nValueWidth, line.TextBox_value.ClientSize.Height);
                    line.TextBox_value.Location = new System.Drawing.Point(
                        this.Padding.Left + nLabelWidth + nBlankWidth + nNameWidth + nStateWidth,
                        y);
                }

                int nHeight = line.TextBox_value.Size.Height;

                if (bSimulate == false)
                {
                    // line.Label_label.AutoSize = true;
                    line.Label_label.Size = new Size(nLabelWidth, nHeight);
                    line.Label_label.Location = new System.Drawing.Point(this.Padding.Left, y);

                    line.Label_Name.AutoSize = true;
                    // line.Label_Name.Size = new Size(nNameWidth, nHeight);
                    line.Label_Name.Location = new System.Drawing.Point(
                        this.Padding.Left + nLabelWidth + nBlankWidth,
                        y);

                    line.Label_state.Size = new Size(nStateWidth, nHeight);
                    line.Label_state.Location = new System.Drawing.Point(
                        this.Padding.Left + nLabelWidth + nBlankWidth + nNameWidth,
                        y);

                }

                y = y + nHeight;
            }

            int nListViewOrgX = this.Padding.Left + nLabelWidth + nBlankWidth + nNameWidth + nStateWidth + nMaxValueWidth + 4 + this.Padding.Right;
            int nListViewOrgY = 0;

            int nListViewWidth = Math.Min(this.Width - nListViewOrgX - 10, 200);
            int nListViewHeight = Math.Max(y+this.Padding.Bottom, 10 * this.DefaultValueFont.Height);  // ��֤�߶Ȳ���̫С

            if (bSimulate == false)
            {
                this.listView_values.Location = new System.Drawing.Point(
                    nListViewOrgX,
                    nListViewOrgY);
                this.listView_values.Size = new Size(nListViewWidth, nListViewHeight);
                this.listView_values.MinimumSize = new Size(50, nListViewHeight);
                this.listView_values.MaximumSize = new Size(1024, 768);
            }

            int nDocumentWith = nListViewOrgX + nListViewWidth;
            int nDocumentHeight = nListViewHeight;

            nResultWidth = nDocumentWith + 100;
            nResultHeight = nDocumentHeight;


            // TODO: �޸�label�Ŀ��?
            if (bSimulate == false)
            {
                this.ResumeLayout(false);
                this.PerformLayout();
            }
        }

        protected override void OnFontChanged(
EventArgs e)
        {
            base.OnFontChanged(e);

            // ��ʹ�������µ�����
            this.m_fontDefaultInfo = null;
            this.m_fontDefaultValue = null;

            // Font textbox_font = new Font("Courier New", this.Font.SizeInPoints, GraphicsUnit.Point);
            for (int i = 0; i < this.templateRoot.Lines.Count; i++)
            {
                TemplateLine line = (TemplateLine)this.templateRoot.Lines[i];

                string strTempValue = line.m_strValue;

                line.TextBox_value.Font = this.DefaultValueFont;
                line.Label_label.Font = this.DefaultInfoFont;
                line.Label_Name.Font = this.DefaultInfoFont;
            }
        }
	}


	public delegate void BeginGetValueListEventHandle(object sender,
	BeginGetValueListEventArgs e);
	public class BeginGetValueListEventArgs: EventArgs
	{
		public string Ref = "";
	}

	public delegate void EndGetValueListEventHandle(object sender,
	EndGetValueListEventArgs e);
	public class EndGetValueListEventArgs: EventArgs
	{
		public string Ref = "";
	}

    //
    public delegate void GetConfigFileEventHandle(object sender,
GetConfigFileEventArgs e);

    public class GetConfigFileEventArgs : EventArgs
    {
        public string Path = "";
        // public bool Found = false;

        public Stream Stream = null;

        public string ErrorInfo = "";
    }

    //
    public delegate void GetConfigDomEventHandle(object sender,
GetConfigDomEventArgs e);

    public class GetConfigDomEventArgs : EventArgs
    {
        public string Path = "";    // [in]

        public XmlDocument XmlDocument = null;  // [out]

        public string ErrorInfo = "";   // [out]
    }


    // ��ú��ʵ��ֵ
    public delegate void ParseMacroEventHandler(object sender,
        ParseMacroEventArgs e);

    public class ParseMacroEventArgs : EventArgs
    {
        public string Macro = "";   // ��
        public bool Simulate = false;   // �Ƿ�Ϊģ�ⷽʽ? ��ģ�ⷽʽ��, ���Ӻ���������Ϊ������Ӻ���ִ��,Ҳ���ǲ���ı�����ֵ
        public string Value = "";   // [out]���ֺ��ֵ
        public string ErrorInfo = "";   // [out]������Ϣ
    }


    public enum CaretPosition
    {
        None = 0,
        FirstChar = 1,
        LastChar = 2,
    }

    /// <summary>
    /// ��ȡһ���ض�ģ��� XML ������¼�
    /// </summary>
    /// <param name="sender">������</param>
    /// <param name="e">�¼�����</param>
    public delegate void GetTemplateDefEventHandler(object sender,
GetTemplateDefEventArgs e);

    /// <summary>
    /// ��ȡһ���ض�ģ��� XML ������¼��Ĳ���
    /// </summary>
    public class GetTemplateDefEventArgs : EventArgs
    {
        /// <summary>
        /// [in] �ֶ���
        /// </summary>
        public string FieldName = "";           // [in]
        /// <summary>
        /// [in] ���ֶ���
        /// </summary>
        public string SubfieldName = "";        // [in]
        /// <summary>
        /// [in] ���ڸ����жϵ��ַ�����һ����ģ���е�ǰ��ȫ������
        /// </summary>
        public string Value = "";   // [in]���ڸ����жϵ��ַ�����һ����ģ���е�ǰ��ȫ������

        /// <summary>
        /// [out] ���ش�����Ϣ
        /// </summary>
        public string ErrorInfo = "";   // [out]������Ϣ
        /// <summary>
        /// [out] �¼����Ƿ�����˶Ը��ֶ�/���ֶ�ģ��Ĵ���
        /// </summary>
        public bool Canceled = false;   // [out]�¼����Ƿ�����˶Ը��ֶ�/���ֶ�ģ��Ĵ���

        /// <summary>
        /// [out] ����ڵ�
        /// </summary>
        public XmlNode DefNode = null;   // [out]����ڵ�
        /// <summary>
        /// [out] ģ�崰�ڱ��⡣һ������������֮������
        /// </summary>
        public string Title = "";       // [out]ģ�崰�ڱ��⡣һ������������֮������
    }

    // 
    public delegate void ResetTitleEventHandler(object sender,
ResetTitleEventArgs e);

    public class ResetTitleEventArgs : EventArgs
    {
        public string Title = "";
    }
}
