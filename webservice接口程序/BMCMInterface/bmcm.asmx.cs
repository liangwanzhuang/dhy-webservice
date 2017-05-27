using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Services;
using DataBaseHelper;

namespace BMCMInterface
{
    /// <summary>
    /// bmcm 的摘要说明
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // 若要允许使用 ASP.NET AJAX 从脚本中调用此 Web 服务，请取消注释以下行。 
    [System.Web.Script.Services.ScriptService]
    public class bmcm : System.Web.Services.WebService
    {
        private static string token = string.Empty;
        LogFiles LogFile = null;
        ConfirmDrugs confirm = null;
        bool ismatch = false;
        bool IsCheck = false;
        [WebMethod]
        public string HelloWorld()
        {
            return "Hello World";
        }

        public bmcm()
        {
            SqlHelper.conn = ConfigurationManager.AppSettings["SQLConn"];
            token = ConfigurationManager.AppSettings["Token"].Trim();
            ismatch = ConfigurationManager.AppSettings["ISMatch"].Trim().ToLower() == "false" ? false : true;
            IsCheck = ConfigurationManager.AppSettings["IsCheck"].Trim().ToLower() == "false" ? false : true;
            LogFile = new LogFiles();
            LogFile.FileName = AppDomain.CurrentDomain.BaseDirectory + "logText.log";
            confirm = new ConfirmDrugs();
        }

        /// <summary>
        /// 获取处方机组信息
        /// </summary>
        /// <param name="bmNo">煎药单号</param>
        /// <param name="toKen">验证key</param>
        /// <returns>执行结果</returns>
        [WebMethod]
        public string Get_MachineRoom(string _bmNo, string _toKen)
        {
            if (_toKen == token)
            {
                string sql = @" select * from ( select p.ID AS 煎药单号,p.Pspnum as 处方号,convert(varchar(60),t.machineid)+'@'+m.machinename as 煎药机编号名称,convert(varchar(60),mr.meRoomNum)+'@'+mr.meRoomName as 煎药室编号名称 from prescription p 
                                inner join tisaneunit t on t.pid =p.id
                                inner join machine m on t.machineid =m.id 
                                left join MedicineRoom mr on m.roomnum =mr.meRoomName ) b where 1=1
                                and 煎药单号=" + _bmNo.Trim();

                SqlDataReader dr = null;
                try
                {
                    dr = SqlHelper.ExecuteReader(SqlHelper.conn, CommandType.Text, sql);
                    if (dr.Read())
                    {
                        string Str = dr[0].ToString() + "," + dr[2] + "," + dr[3];
                        dr.Close();
                        return Str;
                    }
                    else
                    {
                        dr.Close();
                        return "没有相关煎药单号信息!";
                    }
                }
                catch (Exception ex)
                {
                    dr.Close();
                    LogFile.WriteLine("获取煎药室异常：" + ex.Message + sql);
                    return "异常:" + ex.Message;
                }
            }
            else
            {
                return "token未通过验证";
            }
        }


        /// <summary>
        /// 插入药品明细
        /// </summary>
        /// <param name="Hospital_name">医院名称</param>
        /// <param name="Pspnum">处方号</param>
        /// <param name="drugnum">药品编号</param>
        /// <param name="drugname">药品名称</param>
        /// <param name="drugdescription">脚注</param>
        /// <param name="drugposition">药品规格</param>
        /// <param name="drugallnum">单剂量</param>
        /// <param name="drugweight">总剂量</param>
        /// <param name="tienum">贴数</param>
        /// <param name="description">说明</param>
        /// <param name="retailprice">零售价格</param>
        /// <returns></returns>
        [WebMethod]
        public bool Insert_Drug(string Hospital_name, string Pspnum, string drugnum, string drugname, string drugdescription, string drugposition, string drugallnum, string drugweight, string tienum, string description, string retailprice)
        {
            bool resule = false;
            SqlDataReader dr = null;
            StringBuilder strbu = new StringBuilder();

            try
            {
                //根据医院名称获取医院编号
                string sql = "select top 1 id from Hospital where  Hname like '%" + Hospital_name + "%'";
                dr = SqlHelper.ExecuteReader(SqlHelper.conn, CommandType.Text, sql);
                if (dr.Read())
                {
                    string hpid = dr[0].ToString();
                    sql = "select count(id) from drug where hospitalid='" + hpid + "' and Pspnum='" + Pspnum + "' and drugnum='" + drugnum + "'";
                    int c = SqlHelper.ExecuteNonQuery(SqlHelper.conn, CommandType.Text, sql);

                    if (c > 0)
                    {
                        strbu.AppendFormat("update drug set drugdescription='{0}',drugposition='{1}',drugallnum='{2}',drugweight='{3}',tienum='{4}',description='{5}',retailprice='{6}' where Hospitalid='{7}' and Pspnum='{8}' and drugnum='{9}'",
                          drugdescription, drugposition, drugallnum, drugweight, tienum, description, retailprice, hpid, Pspnum, drugnum);
                        int u = SqlHelper.ExecuteNonQuery(SqlHelper.conn, CommandType.Text, strbu.ToString());
                        if (u > 0)
                        {
                            resule = true;
                        }
                        else
                        {
                            LogFile.WriteLine("更新药品失败:" + strbu.ToString());
                        }
                    }
                    else
                    {
                        strbu.AppendFormat("insert into drug(customid,Hospitalid,Pspnum,drugnum,drugname,drugdescription,drugposition,drugallnum,drugweight,tienum,description,retailprice,pid) VALUES ('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}')",
                          0, hpid, Pspnum, drugnum, drugname, drugdescription, drugposition, drugallnum, drugweight, tienum, description, retailprice, 0);
                        int i = SqlHelper.ExecuteNonQuery(SqlHelper.conn, CommandType.Text, strbu.ToString());
                        if (i > 0)
                        {
                            resule = true;
                        }
                        else
                        {
                            LogFile.WriteLine("插入药品失败:" + strbu.ToString());
                        }
                    }
                }
                dr.Close();
                return resule;
            }
            catch (Exception ex)
            {
                LogFile.WriteLine("插入或更新药品失败:" + ex.Message + strbu.ToString());
                dr.Close();
                return resule;
            }
        }


        /// <summary>
        /// 插入处方信息
        /// </summary>
        /// <param name="delnum">委托单号</param>
        /// <param name="Hospital_name">医院名称</param>
        /// <param name="Pspnum">处方号</param>
        /// <param name="name">患者姓名</param>
        /// <param name="sex">性别  1：男2： 女</param>
        /// <param name="age">年龄</param>
        /// <param name="phone">电话</param>
        /// <param name="address">地址</param>
        /// <param name="department">科室</param>
        /// <param name="inpatientarea">病区</param>
        /// <param name="ward">病房号</param>
        /// <param name="sickbed">病床号</param>
        /// <param name="diagresult">诊断结果</param>
        /// <param name="dose">贴数</param>
        /// <param name="takenum">次数</param>
        /// <param name="getdrugtime">取药时间</param>
        /// <param name="getdrugnum">取药号</param>
        /// <param name="decscheme">煎药方案</param>
        /// <param name="oncetime">一煎时间 分钟数</param>
        /// <param name="twicetime">二煎时间 分钟数</param>
        /// <param name="packagenum">包装量</param>
        /// <param name="dotime">上传时间</param>
        /// <param name="doperson">上传人</param>
        /// <param name="dtbcompany">配送公司</param>
        /// <param name="dtbaddress">配送地址</param>
        /// <param name="dtbphone">联系电话</param>
        /// <param name="dtbtype">配送公司</param>
        /// <param name="soakwater">浸泡加水量</param>
        /// <param name="soaktime">浸泡时间</param>
        /// <param name="labelnum">标签数量</param>
        /// <param name="remark">备注信息</param>
        /// <param name="doctor">医生</param>
        /// <param name="footnote">脚注</param>
        /// <param name="ordertime">下单时间</param>
        /// <param name="curstate">当前状态</param>
        /// <param name="decmothed">煎药方式</param>
        /// <param name="takeway">服用方法</param>
        /// <param name="takemethod">服用方式</param>
        /// <param name="RemarksA">备注A</param>
        /// <param name="RemarksB">备注B</param>
        /// <param name="Drug_count">药品数量（有几味药）</param>
        /// <returns></returns>
        [WebMethod]
        public bool Insert_CF(string delnum, string Hospital_name, string Pspnum, string name, string sex, string age, string phone, string address, string department, string inpatientarea, string ward, string sickbed, string diagresult, string dose, string takenum, string getdrugtime, string getdrugnum, string decscheme, string oncetime, string twicetime,
                                string packagenum, string dotime, string doperson, string dtbcompany, string dtbaddress, string dtbphone, string dtbtype, string soakwater, string soaktime, string labelnum, string remark, string doctor, string footnote, string ordertime, string curstate, string decmothed, string takeway, string takemethod, string RemarksA, string RemarksB, string Drug_count, string isDaijian)
        {
            bool resule = false;
            SqlDataReader dr = null;
            StringBuilder strbu = new StringBuilder();
       
                try
                {
                    //根据医院名称获取医院编号
                    string sql = "select top 1 id from Hospital where  Hname like '%" + Hospital_name + "%'";
                    dr = SqlHelper.ExecuteReader(SqlHelper.conn, CommandType.Text, sql);
                    if (dr.Read())
                    {
                        string hpid = dr[0].ToString();
                        sql = "select count(id) from prescription where hospitalid='" + hpid + "' and Pspnum='" + Pspnum + "'";
                        int c = Convert.ToInt32(SqlHelper.ExecuteScalar(SqlHelper.conn, CommandType.Text, sql));

                        if (c > 0)
                        {
                            strbu.AppendFormat("update prescription set name='{0}',sex='{1}',age='{2}',phone='{3}',address='{4}',department='{5}',inpatientarea='{6}',ward='{7}',sickbed='{8}',diagresult='{9}',dose='{10}',takenum='{11}',getdrugtime='{12}',getdrugnum='{13}',decscheme='{14}',oncetime='{15}',twicetime='{16}',packagenum='{17}'," +
                                               "dotime='{18}',doperson='{19}',dtbcompany='{20}',dtbaddress='{21}',dtbphone='{22}',dtbtype='{23}',soakwater='{24}',soaktime='{25}',labelnum='{26}',remark='{27}',doctor='{28}',footnote='{29}',ordertime='{30}',curstate='{31}',decmothed='{32}',takeway='{33}',takemethod='{34}',RemarksA='{35}',RemarksB='{36}',drug_count='{37}',isDaijian='{40}'  where  hospitalid='{38}' and Pspnum='{39}'",
                                                 name, sex, age, phone, address, department, inpatientarea, ward, sickbed, diagresult, dose, takenum, getdrugtime, getdrugnum, decscheme, oncetime, twicetime, packagenum, dotime, doperson, dtbcompany, dtbaddress, dtbphone, dtbtype, soakwater, soaktime, labelnum, remark, doctor, footnote, ordertime, curstate, decmothed, takeway, takemethod, RemarksA, RemarksB, Drug_count, hpid, Pspnum, isDaijian);

                            int u = SqlHelper.ExecuteNonQuery(SqlHelper.conn, CommandType.Text, strbu.ToString());
                            if (u > 0)
                            {
                                resule = true;
                            }
                            else
                            {
                                LogFile.WriteLine("更新处方失败:" + strbu.ToString());
                            }
                        }
                        else
                        {
                            strbu.AppendFormat("insert into prescription(delnum,Hospitalid,Pspnum,name,sex,age,phone,address,department,inpatientarea,ward,sickbed,diagresult,dose,takenum,getdrugtime,getdrugnum,decscheme,oncetime,twicetime,packagenum," +
                                               "dotime,doperson,dtbcompany,dtbaddress,dtbphone,dtbtype,soakwater,soaktime,labelnum,remark,doctor,footnote,ordertime,curstate,decmothed,takeway,takemethod,RemarksA,RemarksB,drug_count,isDaijian) VALUES " +
                                               "('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}','{13}','{14}','{15}','{16}','{17}','{18}','{19}','{20}','{21}','{22}','{23}','{24}','{25}','{26}','{27}','{28}','{29}','{30}','{31}','{32}','{33}','{34}','{35}','{36}','{37}','{38}','{39}','{40}','{41}')",
                                                 delnum, hpid, Pspnum, name, sex, age, phone, address, department, inpatientarea, ward, sickbed, diagresult, dose, takenum, getdrugtime, getdrugnum, decscheme, oncetime, twicetime, packagenum,
                                                 dotime, doperson, dtbcompany, dtbaddress, dtbphone, dtbtype, soakwater, soaktime, labelnum, remark, doctor, footnote, ordertime, curstate, decmothed, takeway, takemethod, RemarksA, RemarksB, Drug_count, isDaijian);
                            int i = SqlHelper.ExecuteNonQuery(SqlHelper.conn, CommandType.Text, strbu.ToString());
                            if (i > 0)
                            {
                                sql = "select top 1 id from prescription where hospitalid ='" + hpid + "' and Pspnum='" + Pspnum + "'";
                                object obj = SqlHelper.ExecuteScalar(SqlHelper.conn, CommandType.Text, sql);
                                if (obj != null)
                                {
                                    string pid = obj.ToString();
                                    sql = "insert into jfInfo(pid,jiefangman,jiefangtime)values('" + pid + "','系统对接','" + dotime + "')";
                                    int i1 = SqlHelper.ExecuteNonQuery(SqlHelper.conn, CommandType.Text, sql);
                                    if (i1 > 0)
                                    {
                                        resule = true;
                                    }
                                }
                            }
                            else
                            {
                                LogFile.WriteLine("插入处方失败:" + strbu.ToString());
                            }
                        }
                    }
                    dr.Close();
                    return resule;
                }
                catch (Exception ex)
                {
                    LogFile.WriteLine("插入或更新处方失败:" + ex.Message + strbu.ToString());
                    dr.Close();
                    return false;
                }
            }
       
            
            

   

        [WebMethod]
        public bool ConfirmDrug(string Hospital_name, string Pspnum)
        {
            bool resule = true;
            SqlDataReader dr = null;
            try
            {
                string sql = "select top 1 id from Hospital where  Hname like '%" + Hospital_name + "%'";
                dr = SqlHelper.ExecuteReader(SqlHelper.conn, CommandType.Text, sql);
                if (dr.Read())
                {
                    string hpid = dr[0].ToString().Trim();
                    sql = "select id,decscheme,dose,takenum,packagenum from prescription where hospitalid='" + hpid + "' and Pspnum='" + Pspnum + "'";
                    SqlDataReader dReader = SqlHelper.ExecuteReader(SqlHelper.conn, CommandType.Text, sql);
                    if (dReader.Read())
                    {
                        string pid = dReader["id"].ToString().Trim();
                        sql = "update drug set pid='" + pid + "'  where (pid='0' or pid='') and hospitalid='" + hpid + "' and Pspnum='" + Pspnum + "'";
                        SqlHelper.ExecuteNonQuery(SqlHelper.conn, CommandType.Text, sql);
                        confirm.confirmDrug(pid, Pspnum, hpid, Hospital_name, ismatch,IsCheck);

                        ReckonAddedWater(dReader);


                        sql = "update prescription set confirmDrug='1' where ID='" + pid + "'";
                        SqlHelper.ExecuteNonQuery(SqlHelper.conn, CommandType.Text, sql);
                    }
                    dReader.Close();
                }
                dr.Close();
            }
            catch (Exception ex)
            {
                LogFile.WriteLine("药品确认录入完成失败:" + ex.Message);
                dr.Close();
                return false;
            }
            return resule;
        }

        /// <summary>
        /// 计算加水量
        /// </summary>
        /// <param name="decscheme"></param>
        /// <returns></returns>
        private void ReckonAddedWater(SqlDataReader dre)
        {
            try
            {
                string s_drug = "select sum(drugweight) from drug where pid='" + dre["id"].ToString().Trim() + "'";
                object obj = SqlHelper.ExecuteScalar(SqlHelper.conn, CommandType.Text, s_drug);
                if (obj != null)
                {
                    int weight = Convert.ToInt32(obj);
                    double soakwater = 0;//泡药加水量
                    //int packnum = Convert.ToInt32(dre["packagenum"].ToString().Trim());//包装量
                    //int taknum = Convert.ToInt32(dre["takenum"].ToString().Trim());//次数
                    int dose = Convert.ToInt32(dre["dose"].ToString().Trim());//付数
                    int decscheme = Convert.ToInt32(dre["decscheme"].ToString().Trim());//煎药方案
                    //if (packnum == 0)
                    //{
                    //    packnum = 200;
                    //}

                    switch (decscheme)
                    {
                        case 1:
                        case 2:
                        case 3:
                        case 81:
                            soakwater = 1.1 * weight + dose * 400 + 400;
                            break;
                        default:
                            soakwater = 1.1 * weight + dose * 240 + 600;
                            break;
                    }
                    string str = "update prescription set soakwater='" + (int)soakwater + "' where id='" + dre["id"].ToString().Trim() + "'";
                    SqlHelper.ExecuteNonQuery(SqlHelper.conn, CommandType.Text, str);
                }
            }
            catch (Exception ex)
            {
                LogFile.WriteLine("计算加水量异常:" + ex.Message);
            }
        }

        /// <summary>
        /// 重打调配条码
        /// </summary>
        /// <param name="Hospital_name"></param>
        /// <param name="Pspnum"></param>
        /// <returns></returns>
        [WebMethod]
        public bool RePrint(string Hospital_name, string Pspnum)
        {
            bool resule = false;
            try
            {
                string sql = "select top 1 id from Hospital where  Hname like '%" + Hospital_name + "%'";
                object htid = SqlHelper.ExecuteScalar(SqlHelper.conn, CommandType.Text, sql);
                if (htid != null)
                {
                    sql = "select top 1 id from prescription where hospitalid ='" + htid.ToString() + "' and Pspnum='" + Pspnum + "'";
                    object obj = SqlHelper.ExecuteScalar(SqlHelper.conn, CommandType.Text, sql);
                    if (obj != null)
                    {
                        string pid = obj.ToString();
                        sql = "update PrescriptionCheckState set printstatus=0 where prescriptionId=" + pid;
                        int i1 = SqlHelper.ExecuteNonQuery(SqlHelper.conn, CommandType.Text, sql);
                        if (i1 > 0)
                        {
                            resule = true;
                        }
                        else
                        {
                            LogFile.WriteLine("重打调配条码失败:" + sql);
                        }
                    }
                }
                return resule;

            }
            catch (Exception ex)
            {
                LogFile.WriteLine("重打调配条码失败:" + ex.Message);
                return resule;
            }
        }


    }
}
