using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.IO;
using System.Web;
using System.Windows.Forms;

///���ܣ�����С���� ��̬

namespace Php
{
    static class Tool
    {
        #region ������
        private const int INTERNET_CONNECTION_MODEM = 1;
        private const int INTERNET_CONNECTION_LAN = 2;
        private const int INTERNET_CONNECTION_PROXY = 4;
        private const int INTERNET_CONNECTION_MODEM_BUSY = 8;
        [DllImport("wininet.dll")]
        //����InternetGetConnectedState���ر���ϵͳ����������״̬
        private static extern bool InternetGetConnectedState(ref int lpdwFlags, int dwReserved);  
           
        /// <summary>
        /// �жϱ������������״̬���жϵ�ǰ�Ƿ�����Internet��
        /// </summary>
        /// <returns></returns>
        public static bool LocalConnectionStatus()
        {
            bool flag = false;
            System.Int32 dwFlag = new Int32();
            if (!InternetGetConnectedState(ref dwFlag, 0))
            {
                //δ����
                flag = false;
            }
            else
            {
                if ((dwFlag & INTERNET_CONNECTION_MODEM) != 0)
                {
                    //���õ��ƽ��������           
                }
                else if ((dwFlag & INTERNET_CONNECTION_LAN) != 0)
                {
                    //������������
                }
                flag = true;
            }
            return flag;
        }
        #endregion

        /// <summary>
        /// ��ȡ������IP
        /// </summary>
        /// <returns></returns>
        public static string getAddressIP()
        {
            ///��ȡ�����
            string AddressIP = string.Empty;
            foreach (IPAddress _IPAddress in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (_IPAddress.AddressFamily.ToString() == "InterNetwork")
                {
                    AddressIP = _IPAddress.ToString();
                }
            }
            return AddressIP;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url">ָ����Url��ַ</param>
        /// <param name="Isbool">�Ƿ�ȥ����ҳԴ���еĻس����з���true ȥ��</param>
        /// <returns></returns>
        static  public string GetHtmlSource(string url,bool Isbool)
        {
            if (url == "")
                return "";

            string charSet = "";
            WebClient myWebClient = new WebClient();


            //��ȡ���������ڶ��� Internet ��Դ��������������֤������ƾ�ݡ�   
            myWebClient.Credentials = CredentialCache.DefaultCredentials;


            byte[] myDataBuffer;
            string strWebData;

            try
            {
                //����Դ�������ݲ������ֽ����顣����@����Ϊ��ַ�м���"/"���ţ�   
                myDataBuffer = myWebClient.DownloadData(@url);
                strWebData = Encoding.Default.GetString(myDataBuffer);
            }
            catch (System.Net.WebException  ) 
            {
                return "";
            }

            //��ȡ��ҳ��ı����ʽ
            Match charSetMatch = Regex.Match(strWebData, "<meta([^<]*)charset=([^<]*)\"", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            string webCharSet = charSetMatch.Groups[2].Value;
            if (charSet == null || charSet == "")
                charSet = webCharSet;

            if (charSet != null && charSet != "" && Encoding.GetEncoding(charSet) != Encoding.Default)
                strWebData = Encoding.GetEncoding(charSet).GetString(myDataBuffer);

            if (Isbool == true)
            {
                strWebData = Regex.Replace(strWebData, @"([\r\n])[\s]+", "", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                strWebData.Replace(@"\r\n", "");
            }

            return strWebData;

        }

        //ȥ���ַ����Ļس����з���
        public static string ClearFlag(string str)
        {
            str = Regex.Replace(str, @"([\r\n])[\s]+", "", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            str.Replace(@"\r\n", "");

            return str;
        }

        

        static private int CountUrl(string WebUrl)
        {
            int Count = 0;
            Match Para = Regex.Match(WebUrl, "{.*}");
            
            return Count;
        }

        //����д��ĸת��Сд��ĸ
        public static string LetterToLower(string str)
        {
            if (str == "" || str == null)
                return "";

            string lowerStr="";
            char c;

            for (int i=0;i<str.Length ;i++)
            {
                c =char .Parse ( str.Substring(i, 1));

                if (Char.IsUpper(c))
                {
                    c = Char.ToLower(c);
                }
                lowerStr += c;
            }

            return lowerStr;
           
        }

        //���ַ����е��ַ���ת��ģ������滻
        //����滻����е�ͷ�Σ���������⻹����λ����֪���Ƿ����һ�ΰ��շ�����������ʽ��Ӧ�����滻
        //������޸Ĵ��࣬����дʵ���ǲ����ѣ��Ǻ�
        public static string ReplaceTrans(string str)
        {
            if (str == "" || str==null )
                return "";

            string conStr="";
            if (Regex.IsMatch(str, "['\"<>&]"))
            {
                Regex re = new Regex("&", RegexOptions.IgnoreCase);
                str = re.Replace(str, "&amp;");
                re = null;

                re = new Regex("<", RegexOptions.IgnoreCase);
                str = re.Replace(str, "&lt;");
                re = null;

                re = new Regex(">", RegexOptions.IgnoreCase);
                str = re.Replace(str, "&gt;");
                re = null;

                re = new Regex("'", RegexOptions.IgnoreCase);
                str = re.Replace(str, "&apos;");
                re = null;

                re = new Regex("\"", RegexOptions.IgnoreCase);
                str = re.Replace(str, "&quot;");
                re = null;
                conStr = str;

            }
            else
            {
                conStr = str;
            }
            return conStr;
        }

        //������ʽת��
        public static string RegexReplaceTrans(string str)
        {
            if (str == "" || str == null)
                return "";

            string conStr = "";
            if (Regex.IsMatch(str, @"[\$\*\[\]\?\\\(\)]"))
            {
                Regex re = new Regex(@"\\", RegexOptions.IgnoreCase);
                str = re.Replace(str, @"\\");
                re = null;

                re = new Regex(@"\$", RegexOptions.IgnoreCase);
                str = re.Replace(str, @"\$");
                re = null;

                //re = new Regex(@"\.", RegexOptions.IgnoreCase);
                //str = re.Replace(str, @"\.");
                //re = null;

                re = new Regex(@"\*", RegexOptions.IgnoreCase);
                str = re.Replace(str, @"\*");
                re = null;

                re = new Regex(@"\[", RegexOptions.IgnoreCase);
                str = re.Replace(str, @"\[");
                re = null;

                re = new Regex(@"\]", RegexOptions.IgnoreCase);
                str = re.Replace(str, @"\]");
                re = null;

                re = new Regex(@"\?", RegexOptions.IgnoreCase);
                str = re.Replace(str, @"\?");
                re = null;

                re = new Regex(@"\(", RegexOptions.IgnoreCase);
                str = re.Replace(str, @"\(");
                re = null;

                re = new Regex(@"\)", RegexOptions.IgnoreCase);
                str = re.Replace(str, @"\)");
                re = null;

                conStr = str;

            }
            else
            {
                conStr = str;
            }
            return conStr;
        }

        //���ڽ��ַ���ת����UTF-8����
        public static string ToUtf8(string str)
        {
            if (str == null)
            {
                return string.Empty;
            }
            else
            {
                char[] hexDigits = {  '0', '1', '2', '3', '4','5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'};

                Encoding utf8 = Encoding.UTF8;
                StringBuilder result = new StringBuilder();

                for (int i = 0; i < str.Length; i++)
                {
                    string sub = str.Substring(i, 1);
                    byte[] bytes = utf8.GetBytes(sub);

                    for (int j = 0; j < bytes.Length; j++)
                    {
                        result.Append("%" + hexDigits[bytes[j] >> 4] + hexDigits[bytes[j] & 0XF]);
                    }
                }

                return result.ToString();
            }
        }

        //��UTF-8����ת�����ַ���
        public static string FromUtf8(string str)
        {
            char[] hexDigits = {  '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'};
            List<byte> byteList = new List<byte>(str.Length / 3);

            if (str != null)
            {
                List<string> strList = new List<string>();
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < str.Length; ++i)
                {
                    if (str[i] == '%')
                    {
                        strList.Add(str.Substring(i, 3));
                    }
                }

                foreach (string tempStr in strList)
                {
                    int num = 0;
                    int temp = 0;
                    for (int j = 0; j < hexDigits.Length; ++j)
                    {
                        if (hexDigits[j].Equals(tempStr[1]))
                        {
                            temp = j;
                            num = temp << 4;
                        }
                    }

                    for (int j = 0; j < hexDigits.Length; ++j)
                    {
                        if (hexDigits[j].Equals(tempStr[2]))
                        {
                            num += j;
                        }
                    }

                    byteList.Add((byte)num);
                }
            }

            return Encoding.UTF8.GetString(byteList.ToArray());
        }

        public static string UTF8ToGB2312(string str)
        {
            try
            {
                Encoding utf8 = Encoding.GetEncoding(65001);
                Encoding gb2312 = Encoding.GetEncoding("gb2312");//Encoding.Default ,936
                byte[] temp = utf8.GetBytes(str);
                byte[] temp1 = Encoding.Convert(utf8, gb2312, temp);
                string result = gb2312.GetString(temp1);
                return result;
            }
            catch (Exception)//(UnsupportedEncodingException ex)
            {
                return null;
            }
        }
        /// <summary>
        /// ���������Soukey��ժ�����·��
        /// </summary>
        /// <param name="absPath"></param>
        /// <returns></returns>
        public static string GetRelativePath(string absPath)
        {
            string mainDir = Program.getPrjPath();

            //if (!mainDir.EndsWith("\\"))
            //{
            //    mainDir += "\\";
            //}

            int intIndex = -1, intPos = mainDir.IndexOf('\\');

            while (intPos >= 0)
            {
                intPos++;
                if (string.Compare(mainDir, 0, absPath, 0, intPos, true) != 0) break;
                intIndex = intPos;
                intPos = mainDir.IndexOf('\\', intPos);
            }

            if (intIndex >= 0)
            {
                absPath = absPath.Substring(intIndex);
                intPos = mainDir.IndexOf("\\", intIndex);
                while (intPos >= 0)
                {
                    absPath = "..\\" + absPath;
                    intPos = mainDir.IndexOf("\\", intPos + 1);
                }
            }

            return absPath;
        }
        /// <summary>
        /// �ж�ָ�����ļ����ڵ�Ŀ¼�Ƿ���ڣ��������������
        /// </summary>
        /// <param name="strDir">����Ĳ����������ļ�����������ļ���������"\"��β</param>
        public static void CreateDirectory(string strDir)
        {
            //��Ҫ��ȡ�ļ�Ŀ¼
            strDir = Path.GetDirectoryName(strDir);

            if (!Directory.Exists(strDir))
            {
                //������Ŀ¼
                Directory.CreateDirectory(strDir);
            }
        }
        ///<summary>
        ///�������õ����ڼ��켸Сʱ������
        ///</summary
        ///<param name="t">����</param>
        ///<param name="type">0��ת������룬1:ת���󲻴���</param>
        ///<returns>���켸Сʱ���ּ���</returns>
        public static string parseTimeSeconds(int t, int type = 0)
        {
            string r = "";
            int day, hour, minute, second;
            if (t >= 86400) //��,
            {
                day = Convert.ToInt16(t / 86400);
                hour = Convert.ToInt16((t % 86400) / 3600);
                minute = Convert.ToInt16((t % 86400 % 3600) / 60);
                second = Convert.ToInt16(t % 86400 % 3600 % 60);
                if (type == 0)
                    r = day + ("��") + hour + ("ʱ") + minute + ("��") + second + ("��");
                else
                    r = day + ("��") + hour + ("ʱ") + minute + ("��");

            }
            else if (t >= 3600)//ʱ,
            {
                hour = Convert.ToInt16(t / 3600);
                minute = Convert.ToInt16((t % 3600) / 60);
                second = Convert.ToInt16(t % 3600 % 60);
                if (type == 0)
                    r = hour + ("ʱ") + minute + ("��") + second + ("��");
                else
                    r = hour + ("ʱ") + minute + ("��");
            }
            else if (t >= 60)//��
            {
                minute = Convert.ToInt16(t / 60);
                second = Convert.ToInt16(t % 60);
                r = minute + ("��") + second + ("��");
            }
            else
            {
                second = Convert.ToInt16(t);
                r = second + ("��");
            }
            return r;
        }
        /// <summary>
        /// ��֤�����Ƿ�Ϸ�
        /// </summary>
        /// <param name="str">ָ���ַ���</param>
        /// <returns></returns>
        public static bool IsDomain(string str)
        {
            string pattern = @"^(http://|https://)([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?$";
            Regex reg = new Regex(pattern);
            return reg.IsMatch(str);
        }
        // <summary>
        /// <summary>
        /// �ַ���תUnicode
        /// </summary>
        /// <param name="source">Դ�ַ���</param>
        /// <returns>Unicode�������ַ���</returns>
        public static string StringToUnicode(string source)
        {
            byte[] bytes = Encoding.Unicode.GetBytes(source);
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i += 2)
            {
                stringBuilder.AppendFormat("\\u{0}{1}", bytes[i + 1].ToString("x").PadLeft(2, '0'), bytes[i].ToString("x").PadLeft(2, '0'));
            }
            return stringBuilder.ToString();
        }
        /// <summary>
        /// Unicodeת�ַ���
        /// </summary>
        /// <param name="source">����Unicode������ַ���</param>
        /// <returns>�����ַ���</returns>
        public static string UnicodeToString(string source)
        {
            return new Regex(@"\\u([0-9A-F]{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled).Replace(source, x => string.Empty + Convert.ToChar(Convert.ToUInt16(x.Result("$1"), 16)));
        }
    }
}
