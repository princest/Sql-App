using EasyPump.Importing;
using Oracle.DataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace EasyPump.Controllers
{
    public class ImportsController : ApiController
    {
        // GET: api/Imports
        [AllowAnonymous]
        public Task<object> Post()
        {
            string password;
            if (!Request.Content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotAcceptable, "Invalid Request!"));
            }

            try
            {
                var httpRequest = HttpContext.Current.Request;
                var keys = httpRequest.Form;
                password = keys["password"];
                if (!checkpassword(password)) throw new ApplicationException("密码错误!");
            }
            catch (Exception e)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, e.Message));
            }
            try
            {
                var streamProvider = new MyMultipartFormDataStreamProvider("importing/uploads", Request.RequestUri.Authority);
                //Request.Content.LoadIntoBufferAsync().Wait();
                return Request.Content.ReadAsMultipartAsync(streamProvider).
                    ContinueWith<object>(res =>
                    {
                        object json;
                        var context = res.Result.FileData.Select(i =>
                        {
                            DataSet ds; List<String> cols; List<Object[]> data; List<Type> colTypes;
                            var phy_path = i.LocalFileName.Replace("\"", "");
                            var rname = i.Headers.ContentDisposition.FileName;
                            var test = i.Headers.ContentDisposition.ModificationDate;
                            try
                            {
                                construction(phy_path, out ds);
                                using (ds)
                                {
                                    SqlFace.Models.DynmicService.GetTableRowsDataCompact(ds.Tables[0], out cols, out data, out colTypes);
                                }
                                FileInfo df = res.Result.fnmap[rname];
                                var guid = Guid.NewGuid().ToString();
                                var svc = new SqlFace.Models.OrmService(Config.AppConfigs.sqlfaceconn);
                                svc.Create(new Imp_Management
                                {
                                    id = guid,
                                    create_date = df.CreationTime,
                                    file_name = rname.Trim('"'),
                                    length = df.Length,
                                    uploader = "anonymous",
                                    last_modified_date = df.LastWriteTime,
                                    phy_path = phy_path,
                                    addHeads = null,
                                    total = data.Count
                                });
                                svc.Dispose();
                                json = new { credit = guid, cols = cols, data = data, colTypes = colTypes, total = data.Count };
                                return json;
                            }
                            catch (Exception e)
                            {
                                json = new { hasError = true, Message = e.Message };
                                return json;
                            }
                        });
                        return context;
                    });
            }
            catch (Exception e)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e.Message));
            }
        }


        private bool checkpassword(string password)
        {
            var pass = Utils.DBTool.getStringByRunQuery("select count(1) from Fetch_Templ_Setting where sts='S' and tmpl_code ='custom1' and pwd='" + password + "'");
            return (int.Parse(pass) == 1);

        }
        #region CORS UPLOAD
        /// <summary>
        /* ---------------------------acebdf13572468
        Content-Disposition: form-data; name="fieldNameHere"; filename="popup_2.png"
        Content-Type: image/png
        <@INCLUDE *C:\Users\Administrator\Desktop\Android 1080P\pic\popup_2.png*@>
        ---------------------------acebdf13572468
        Content-Disposition: form-data; name="fieldNameHere"; filename="popup_3.png"
        Content-Type: image/png
        <@INCLUDE *C:\Users\Administrator\Desktop\Android 1080P\pic\popup_3.png*@>
        //---------------------------acebdf13572468--*/
        /// </summary>
        /// <param name="url"></param>
        /// <param name="filepath"></param>
        /// <returns></returns>
        public string DoCorsUpload(string url, string filepath, string mediaType)
        {
            if (Convert.ToBoolean(ConfigurationManager.AppSettings["upload-middler-for-test"]))
                return "in test";

            var timeBoundary = DateTime.Now.Ticks.ToString("x");

            //Identificate separator
            string boundary = "---------------------------" + timeBoundary;
            //Encoding
            byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

            //Creation and specification of the request
            HttpWebRequest httpwebreq = (HttpWebRequest)WebRequest.Create(url); //sVal is id for the webService
            httpwebreq.ContentType = "multipart/form-data; boundary=" + boundary;
            httpwebreq.Method = "POST";
            httpwebreq.KeepAlive = true;
            httpwebreq.Credentials = System.Net.CredentialCache.DefaultCredentials;

            //如果需要鉴权..
            //string sAuthorization = "login:password";//AUTHENTIFICATION BEGIN
            //byte[] toEncodeAsBytes = System.Text.ASCIIEncoding.ASCII.GetBytes(sAuthorization);
            //string returnValue = System.Convert.ToBase64String(toEncodeAsBytes);
            //wr.Headers.Add("Authorization: Basic " + returnValue); //AUTHENTIFICATION END
            //--------------------------------------end of headers--------------------------

            //拼接body流
            using (Stream rs = httpwebreq.GetRequestStream())
            {

                ////非文件域,非文件格式
                //---------------------------acebdf13572468
                //string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
                //foreach (string key in nvc.Keys)
                //{
                //    rs.Write(boundarybytes, 0, boundarybytes.Length);
                //    string formitem = string.Format(formdataTemplate, key, nvc[key]);
                //    byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(formitem);
                //    rs.Write(formitembytes, 0, formitembytes.Length);
                //}

                //写body,以 boundary开始.
                //---------------------------acebdf13572468
                rs.Write(boundarybytes, 0, boundarybytes.Length);

                string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
                string header = string.Format(headerTemplate, "file", timeBoundary, mediaType);
                byte[] header_bytes = System.Text.Encoding.UTF8.GetBytes(header);
                rs.Write(header_bytes, 0, header_bytes.Length);

                FileStream fileStream = new FileStream(filepath, FileMode.Open, FileAccess.Read);

                byte[] buffer = new byte[4096];
                int bytesRead = 0;
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    rs.Write(buffer, 0, bytesRead);
                }
                fileStream.Close();

                byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
                rs.Write(trailer, 0, trailer.Length);
                rs.Close();
                //---------------------------acebdf13572468--
            }
            WebResponse httpwresp = null;
            try
            {
                //Get the response
                httpwresp = httpwebreq.GetResponse();
                using (Stream stream2 = httpwresp.GetResponseStream())
                using (StreamReader responseRead = new StreamReader(stream2))
                    return responseRead.ReadToEnd();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        public static void construction(string savePath, out DataSet ds)
        {

            var ext = System.IO.Path.GetExtension(savePath);
            switch (ext)
            {
                case ".xls":
                case ".xlsx":
                    ds = OleImport.ImportExcelXLS(savePath, true);//Excel必须包含列头即使第一行是数据(必须是文本,否则被吃掉变成'Fx')
                    break;
                case ".csv":
                    //切记,微软的Jet大坑!                    
                    ds = OleImport.ReadDelimitedTextFile_To_DataSet(savePath, DelimiterType.CsvDelimited);
                    break;
                case ".txt":
                case ".tsv":
                    ds = OleImport.ReadDelimitedTextFile_To_DataSet(savePath, DelimiterType.TabDelimited);
                    break;
                //case ".csv":
                //    ds = OleImport.Convert(savePath, "import", ",");
                //    ds = OleImport.Convert(savePath, "import", "\t");
                default:
                    throw new ApplicationException("不识别的格式!");
            };
            //if (addHead)
            //    ds = CommonImport.wrap(ds);

        }
        public string Post(string put,[FromBody]Needs needs)
        {
            var ret = "提交失败,请联系系统管理员:{0}";
            DataSet ds;
            DateTime now = DateTime.Now;
            using (var svc = new SqlFace.Models.OrmService(Config.AppConfigs.sqlfaceconn))
            {
                var t = svc.GetById<Imp_Management>(needs.guid);
                if (svc.FilterWhere<Imp_4g_Log>(s => s.import_id == t.id).Count > 0)
                    return string.Format("不能重复导入多次");
                construction(t.phy_path , out ds);
                using (IDbTransaction trans = svc.theConnection.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    try
                    {
                        string nbr, lvl;
                        DataTable dt = ds.Tables[0];
                        t.addHeads = needs.addHeads;
                        t.mem = needs.mem;
                        t.last_modified_date = now;
                        svc.Update<Imp_Management>(t, new List<string> { "addHeads", "mem", "last_modified_date" }, s => s.id == t.id);

                        if (needs.addHeads)
                        {
                            nbr = dt.Columns[needs.indexes[0]].ColumnName;
                            lvl = dt.Columns[needs.indexes[1]].ColumnName;
                            svc.Create<Imp_4g_Log>(new Imp_4g_Log { log_id = Guid.NewGuid().ToString(), import_id = t.id, phone_number = nbr, pn_level_id = lvl, create_date = now, commit_date = null, old_pn_level_id = null, remarks = null, rsc_cd = null });
                        }

                        foreach (DataRow dr in dt.Rows)
                        {
                            nbr = dr[needs.indexes[0]].ToString();
                            lvl = dr[needs.indexes[1]].ToString();
                            svc.Create<Imp_4g_Log>(new Imp_4g_Log { log_id = Guid.NewGuid().ToString(), import_id = t.id, phone_number = nbr, pn_level_id = lvl, create_date = now, commit_date = null, old_pn_level_id = null, remarks = null, rsc_cd = null });
                        }
                    }
                    catch (Exception e)
                    {
                        trans.Rollback();
                        return string.Format("导入文件中存在重复号码", e.Message);
                    }
                    trans.Commit();
                    try
                    {
                        Dictionary<string, OracleParameter> cmdp = new Dictionary<string, OracleParameter>();
                        cmdp.Add("import_id", new OracleParameter("sql_text", OracleDbType.Varchar2, 400, t.id, System.Data.ParameterDirection.Input));
                        cmdp.Add("returnstr", new OracleParameter("returnstr", OracleDbType.Varchar2, 400, null, System.Data.ParameterDirection.Output));
                        cmdp.Add("stats", new OracleParameter("stats", OracleDbType.Varchar2, 400, null, System.Data.ParameterDirection.Output));
                        Utils.DBTool.RunOracleProcedure("hbwh_oss_admin.Import_4gNbr_with_lvl", ref cmdp);
                        var returnstr = (null == cmdp["returnstr"].Value) ? null : cmdp["returnstr"].Value.ToString();
                        var stats = (null == cmdp["stats"].Value) ? null : cmdp["stats"].Value.ToString();
                        if (returnstr == null || returnstr.ToLower() == "null")
                            return "提交成功,"+stats;
                        else
                            return string.Format(ret, returnstr);
                    }
                    catch
                    {
                        try //调用异常,回滚数据,让用户可以再次尝试
                        {
                            var rt = Utils.DBTool.runCommands(new List<string> { string.Format("delete imp_log where import_id = '{0}'", t.id) });
                            throw;
                        }
                        catch
                        {
                            throw;
                        };
                    }
                }
            }
        }
    }

    public class MyMultipartFormDataStreamProvider : MultipartFormDataStreamProvider
    {
        public readonly Dictionary<string, FileInfo> fnmap;
        private string Authority;
        private string serverBase;

        public MyMultipartFormDataStreamProvider(string uploadFolder, string Authority)
            : base(HttpContext.Current.Server.MapPath("~/" + uploadFolder))
        {
            fnmap = new Dictionary<string, FileInfo>();
            this.Authority = Authority + "/" + uploadFolder + "/";
            this.serverBase = HttpContext.Current.Server.MapPath("~/" + uploadFolder + "/");
        }

        // Summary:
        //     Gets the name of the local file which will be combined with the root path
        //     to create an absolute file name where the contents of the current MIME body
        //     part will be stored.
        //     默认是guid作为文件名的.如果重载,那么可以制定一个有意义的文件名
        // Parameters:
        //   headers:
        //     The headers for the current MIME body part.
        // Returns:
        //     A relative filename with no path component.
        public override string GetLocalFileName(System.Net.Http.Headers.HttpContentHeaders headers)
        {
            var oldname = headers.ContentDisposition.FileName;
            var posible_full_name = (!string.IsNullOrWhiteSpace(oldname) ? oldname : "NoName").Replace("\"", string.Empty);
            var nicename = Path.GetFileNameWithoutExtension(posible_full_name);
            var nicenameExt = Path.GetExtension(posible_full_name);
            var hn = serverBase + posible_full_name;
            Random r = new Random();
            int i = 0;
            while (File.Exists(hn))
            {
                i++;
                posible_full_name = string.Format(nicename + "({0}{1}{2}-{3}{4}{5}-{6})" + nicenameExt,
                    DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,
                    DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second,
                    i);
                hn = serverBase + posible_full_name;
            }
            if (!fnmap.ContainsKey(oldname))
            {
                fnmap.Add(oldname, new FileInfo(hn));//Authority + posible_full_name 图片
            }
            return posible_full_name;
        }

    }
}