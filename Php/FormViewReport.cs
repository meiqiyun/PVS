using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

//htmlɨ�豨��ģ��
namespace Php
{
    public partial class FormViewReport : Form
    {
        public FormViewReport(String url)
        {
            InitializeComponent();
            webBrowser_report.Url = new Uri(url); ;
            webBrowser_report.Update(); 
        }
    }
}