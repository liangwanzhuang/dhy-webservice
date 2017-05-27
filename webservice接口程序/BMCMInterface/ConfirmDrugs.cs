using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Web;
using DataBaseHelper;

namespace BMCMInterface
{
    public class ConfirmDrugs
    {

        public bool confirmDrug(string pid, string pspnum, string Hospitalid, string HospitalName, bool Ismatch,bool IsCheck)
        {
            bool reslue = true;
            string sql = "select count(id) from  drug  where pid='" + pid + "'";
            int p = Convert.ToInt32(SqlHelper.ExecuteScalar(SqlHelper.conn, CommandType.Text, sql));
            if (p > 0 && Ismatch)//ismatch=false时忽略未匹配的药品
            {
                int type = 0;
                object obj = SqlHelper.ExecuteSPOutput(SqlHelper.conn, "sp_drug_matching_type", Hospitalid, pspnum);
                if (obj != null)
                {
                    type = Convert.ToInt32(obj.ToString());
                }
                reslue = findNotCheckAndMatchRecipeDrugInfoToMatch_1(pid, Hospitalid, HospitalName, type);
            }

            if (reslue)
            {
               
                //sql = "select isneedcheck from isneedcheck";
                //string isneedcheck = "";
                //isneedcheck = SqlHelper.ExecuteScalar(SqlHelper.conn, CommandType.Text, sql).ToString();

                if (IsCheck)//IsCheck为true,需要经过审核以后打印;若为false，不需要经过审核即可打印
                {
                    update_p_status_by_pid(pid.ToString(), "未审核");
                    updatePrescriptionStatus(pid, "0", "系统审核", "101");
                }
                 else
                {
                    update_p_status_by_pid(pid.ToString(), "已审核");
                    updatePrescriptionStatus(pid, "1", "系统审核", "101");
                }
            }
            else
            {
                update_p_status_by_pid(pid, "未匹配");
                updatePrescriptionStatus(pid, "0", "系统审核", "101");
            }
            return reslue;
        }


        public bool findNotCheckAndMatchRecipeDrugInfoToMatch_1(string pid, string hisid, string hisname, int hisType)
        {
            bool resule = true;
            string hid = hisid;
            string hname = hisname;

            string s_drug = "select distinct drugname,drugnum from drug where pid='" + pid + "'";
            DataSet ds = SqlHelper.ExecuteDataset(SqlHelper.conn, CommandType.Text, s_drug);
            DataTable dt = null;
            if (ds != null)
            {
                dt = ds.Tables[0];
            }
            //插入匹配
            string hdrugstr = string.Empty;
            string hdrugnum = string.Empty;
            foreach (DataRow dr in dt.Rows)
            {
                hdrugstr = dr["drugname"].ToString().Trim();
                hdrugnum = dr["drugnum"].ToString().Trim();

                //查询药品匹配列表中是否存在
                string ypkid = "";
                string strSql_b = "";
                if (hisType == 1) //饮片库匹配
                {
                    string sql_h = " select top 1 id  from Hospital  where Hname='" + "饮片库" + "'";
                    object obj = SqlHelper.ExecuteScalar(SqlHelper.conn, CommandType.Text, sql_h);
                    if (obj != null)
                    {
                        ypkid = obj.ToString();
                    }
                    strSql_b = "select count(id) as mid from DrugMatching where  hospitalId='" + ypkid + "' and hdrugNum='" + hdrugnum + "'";
                }
                if (hisType == 2) //医院匹配匹配
                {
                    strSql_b = "select count(id) as mid from DrugMatching where  and hospitalId='" + hid + "' and hdrugNum='" + hdrugnum + "'";
                }
                int mid = Convert.ToInt32(SqlHelper.ExecuteScalar(SqlHelper.conn, CommandType.Text, strSql_b).ToString());
                if (mid > 0)
                {
                    continue;
                }
                if (hisType == 2) //医院匹配类型不自动匹配
                {
                    continue;
                }
                //获取药品管理是否存在
                string sy_drug = "select top 1 drugcode,drugname from drugadmin where drugcode ='" + hdrugnum + "'";
                SqlDataReader adr = SqlHelper.ExecuteReader(SqlHelper.conn, CommandType.Text, sy_drug);
                if (adr.Read())
                {
                    //插入匹配
                    string ypcstr = adr["drugname"].ToString();
                    string ypcnum = adr["drugcode"].ToString();

                    StringBuilder strBu = new StringBuilder();
                    //if (hisType == 2) //医院匹配匹配
                    //{
                    //    strBu.AppendFormat("insert into DrugMatching(hospitalName,hospitalId,hdrugNum,hdrugName,ypcdrugNum,ypcdrugName,ypcdrugPositionNum) values ('{0}','{1}','{2}','{3}','{4}','{5}','{6}')",
                    //                        hname, hid, hdrugnum, hdrugstr, ypcnum, ypcstr, 0);

                    //}
                    if (hisType == 1) //饮片库匹配
                    {
                        strBu.AppendFormat("insert into DrugMatching(hospitalName,hospitalId,hdrugNum,hdrugName,ypcdrugNum,ypcdrugName,ypcdrugPositionNum) values ('{0}','{1}','{2}','{3}','{4}','{5}','{6}')",
                                           "饮片库", ypkid, hdrugnum, hdrugstr, ypcnum, ypcstr, 0);
                    }
                    int dm = SqlHelper.ExecuteNonQuery(SqlHelper.conn, CommandType.Text, strBu.ToString());
                    if (dm < 1)
                    {
                        resule = false;
                    }
                }
                else
                {
                    resule = false;
                }
                adr.Close();
            }
            return resule;
        }

        public void update_p_status_by_pid(string p_id, string curstate)
        {
            string sql = "update prescription set curstate='" + curstate + "'" + " where id='" + p_id.Trim() + "'";
            SqlHelper.ExecuteNonQuery(SqlHelper.conn, CommandType.Text, sql);

        }

        public int updatePrescriptionStatus(string pid, string status, string name, string emid)
        {
            string sql = "select count(id) as cid from PrescriptionCheckState where prescriptionId ='" + pid + "'";
            object es = SqlHelper.ExecuteScalar(SqlHelper.conn, CommandType.Text, sql);
            int resul = 0;
            if (es != null)
            {
                resul = Convert.ToInt32(es.ToString());
            }

            if (resul > 0)
            {
                sql = "update  PrescriptionCheckState  set checkStatus ='" + status + "'" + " where prescriptionId='" + pid + "'";
            }
            if (resul == 0)
            {
                sql = "insert into PrescriptionCheckState(prescriptionId,checkStatus,refusalreason,tisaneNumber,PartyTime,PartyPer,employeeid) values('" + pid + "','" + status + "','" + "" + "','" + pid + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + name + "','" + emid + "')";
            }
            return SqlHelper.ExecuteNonQuery(SqlHelper.conn, CommandType.Text, sql);
        }

    }
}