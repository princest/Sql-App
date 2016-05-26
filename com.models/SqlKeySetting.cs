using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlFace.Models
{
    public class SqlKeySetting : UniqueFilterable
    {
        public int Id { get; set; }
        public string SQLKEY { get; set; }
        public string SQL { get; set; }
        public string DBCONN { get; set; }
        public string STS { get; set; }

        public string CLASS { get; set; }
        public string AUTHOR { get; set; }
        public object GetId()
        {
            return Id;
        }

     

        public string STYLEFUNC { get; set; }
        public string ALLOWED_DML { get; set; }
        public string DML_ENTITY { get; set; }
        public string DML_COLS { get; set; }
        public string DML_WHERE_COLS { get; set; }
        public string PRIVATES { get; set; }
        [ServiceStack.DataAnnotations.Ignore]
        public bool Authorized { get; private set; }
        public bool EvalAuthorized(string username, List<string> rolename)
        {
            //计算模型
            if (string.IsNullOrWhiteSpace(AUTHOR))
                Authorized = true;
            else
            {
                Authorized = false;
                AuthorizeObject jo;
                try
                {
                    jo = (AuthorizeObject)JsonSerializer.DeserializeFromString(AUTHOR.Replace('\'','"'), typeof(AuthorizeObject));

                    AuthorizeObject.ColorEnum color = (AuthorizeObject.ColorEnum)
                        Enum.Parse(typeof(AuthorizeObject.ColorEnum), jo.COLOR, true);
                    if (color == AuthorizeObject.ColorEnum.WHITE) //白名单
                    {
                        Authorized = passFitler(jo.WHITE, rolename, username);
                    }
                    else if (color == AuthorizeObject.ColorEnum.BLACK)//黑名单
                    {
                        Authorized = !passFitler(jo.BLACK, rolename, username);
                    }
                    else
                    { //双重过滤
                        Authorized = passFitler(jo.WHITE, rolename, username);
                        if (Authorized)
                            Authorized = !passFitler(jo.BLACK, rolename, username);
                    }
                }
                catch
                {
                }
            }
            return Authorized;
        }
        private bool passFitler(AuthorizeObject.ColorSetting colorsetting,
            List<string> rolename, string username)
        {
            bool annoymouserole = (rolename == null || rolename.Count == 0);
            bool annoymouseuser = string.IsNullOrWhiteSpace(username);
            bool rolefilter, userfilter;
            bool pass;
            if (null == (colorsetting.U) && null == (colorsetting.R))
            {
                return false;//没有配置,返回不匹配,白名单时,没有名单,最终返回false;黑名单,也没有,那么也是false,最终true
            }
            //或 与
            AuthorizeObject.LogicEnum logic;
            if (string.IsNullOrWhiteSpace(colorsetting.LOGIC))
            {
                logic = AuthorizeObject.LogicEnum.and;//默认为与
            }
            else
                logic = (AuthorizeObject.LogicEnum)
                       Enum.Parse(typeof(AuthorizeObject.LogicEnum), colorsetting.LOGIC, true);

            if (null == (colorsetting.R))
            {
                rolefilter = true;//没配置
            }
            else
            {
                rolefilter = colorsetting.R.Count > 0 && (
                    colorsetting.R.Contains("?") && annoymouserole ||
                    colorsetting.R.Contains("*") && !annoymouserole ||
                    colorsetting.R.Intersect(rolename).Count() > 0
                );
            }
            if (null == (colorsetting.U))
            {
                userfilter = true;
            }
            else
            {
                userfilter = colorsetting.U.Count > 0 && (
                    colorsetting.U.Contains("?") && annoymouseuser ||
                    colorsetting.U.Contains("*") && !annoymouseuser ||
                    colorsetting.U.Contains(username)
                );
            }
          

            if (logic == AuthorizeObject.LogicEnum.or)
            {
                pass = rolefilter || userfilter;
            }
            else
            {
                pass = rolefilter && userfilter;
            }
            return pass;
        }

    }
    public class AuthorizeObject
    {
        public class ColorSetting
        {
            public List<string> R { get; set; }
            public List<string> U { get; set; }
            public string LOGIC { get; set; }
        }
        public enum ColorEnum
        {
            WHITE,
            BLACK,
            BOTH,
        }
        public enum LogicEnum
        {
            and,
            or
        }
        public string COLOR { get; set; }
        public ColorSetting WHITE { get; set; }
        public ColorSetting BLACK { get; set; }

    }

}
