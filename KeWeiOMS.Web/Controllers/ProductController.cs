﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using KeWeiOMS.Domain;
using KeWeiOMS.NhibernateHelper;
using NHibernate;

namespace KeWeiOMS.Web.Controllers
{
    public class ProductController : BaseController
    {
        public ViewResult Index()
        {
            return View();
        }

        public ActionResult ProductProfits()
        {
            return View();
        }
        public ActionResult Create()
        {
            return View();
        }

        public ActionResult SKUCodeIndex()
        {
            return View();
        }

        public ActionResult ImportPic()
        {
            return View();
        }

        public ActionResult ImportProduct()
        {
            return View();
        }

        public ActionResult WarningPurchaseList()
        {
            return View();
        }
        [HttpPost]
        public ActionResult WarningList(string order, string sort)
        {
            List<PurchaseData> list = new List<PurchaseData>();
            IList<WarehouseStockType> stocks = new List<WarehouseStockType>();
            IList<PurchasePlanType> plans = new List<PurchasePlanType>();
            IList<ProductType> products =
                NSession.CreateQuery(
                    " From ProductType p where (round((SevenDay/7*0.5+Fifteen/15*0.3+ThirtyDay/30*0.2),0)*5)>( select SUM(Qty) from WarehouseStockType where SKU= p.SKU)")
                    .List<ProductType>();
            string ids = "";
            foreach (var p in products)
            {
                ids += "'" + p.SKU + "',";
            }
            stocks =
                NSession.CreateQuery("from WarehouseStockType where SKU in(" + ids.Trim(',') + ")").List<WarehouseStockType>();
            plans = NSession.CreateQuery("from PurchasePlanType where Status not in('异常','已收到')  and SKU in(" + ids.Trim(',') + ")").List<PurchasePlanType>();
            foreach (var p in products)
            {
                PurchaseData data = new PurchaseData();
                data.ItemName = p.ProductName;
                data.SKU = p.SKU;
                data.SPic = p.SPicUrl;
                data.SevenDay = p.SevenDay;
                data.FifteenDay = p.Fifteen;
                data.ThirtyDay = p.ThirtyDay;
                data.WarningQty =
                    Convert.ToInt32(Math.Round(((p.SevenDay / 7) * 0.5 + p.Fifteen / 15 * 0.3 + p.ThirtyDay / 30 * 0.2), 0) * 5);
                if (data.WarningQty < 1)
                {
                    data.WarningQty = 1;
                }
                data.IsImportant = 0;
                data.AvgQty = Math.Round(((p.SevenDay / 7) * 0.5 + p.Fifteen / 15 * 0.3 + p.ThirtyDay / 30 * 0.2), 2);
                WarehouseStockType stock = stocks.First(x => x.SKU.Trim().ToUpper() == p.SKU.Trim().ToUpper());
                if (stock != null)
                {
                    data.NowQty = stock.Qty;
                }

                if (Math.Round(((p.SevenDay / 7) * 0.5 + p.Fifteen / 15 * 0.3 + p.ThirtyDay / 30 * 0.2), 0) * 3 < data.NowQty)
                {
                    data.IsImportant = 1;
                }

                data.BuyQty = plans.Where(x => x.SKU.Trim().ToUpper() == p.SKU.Trim().ToUpper()).Sum(x => x.Qty);
                if ((data.NowQty + data.BuyQty - data.WarningQty) < 0)
                {
                    data.NeedQty = Convert.ToInt32(data.AvgQty * 10);
                    list.Add(data);
                }
            }
            return Json(new { total = list.Count, rows = list });
        }

        [HttpPost]
        public ActionResult ImportProduct(string fileName)
        {
            DataTable dt = OrderHelper.GetDataTable(fileName);
            IList<WarehouseType> list = NSession.CreateQuery(" from WarehouseType").List<WarehouseType>();
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                ProductType p = new ProductType { CreateOn = DateTime.Now };
                p.SKU = dt.Rows[i]["SKU"].ToString();
                p.Status = ProductStatusEnum.销售中.ToString();
                p.ProductName = dt.Rows[i]["名称"].ToString();
                p.Category = dt.Rows[i]["分类"].ToString();
                p.Standard = dt.Rows[i]["规格"].ToString();
                p.Price = Convert.ToDouble(dt.Rows[i]["价格"]);
                p.Weight = Convert.ToInt16(dt.Rows[i]["重量"]);
                p.Long = Convert.ToInt16(dt.Rows[i]["长"]);
                p.Wide = Convert.ToInt16(dt.Rows[i]["宽"]);
                p.High = Convert.ToInt16(dt.Rows[i]["高"]);
                p.Location = dt.Rows[i]["库位"].ToString();
                p.OldSKU = dt.Rows[i]["旧SKU"].ToString();
                p.HasBattery = Convert.ToInt32(dt.Rows[i]["电池"].ToString());
                p.IsElectronic = Convert.ToInt32(dt.Rows[i]["电子"].ToString());
                p.IsLiquid = Convert.ToInt32(dt.Rows[i]["液体"].ToString());
                p.PackCoefficient = Convert.ToInt32(dt.Rows[i]["包装系数"].ToString());
                p.Manager = dt.Rows[i]["管理人"].ToString();
                NSession.SaveOrUpdate(p);
                //
                //在仓库中添加产品库存
                //
                foreach (var item in list)
                {
                    WarehouseStockType stock = new WarehouseStockType();
                    stock.Pic = p.SPicUrl;
                    stock.WId = item.Id;
                    stock.Warehouse = item.WName;
                    stock.PId = p.Id;
                    stock.SKU = p.SKU;
                    stock.Title = p.ProductName;
                    stock.Qty = 0;
                    stock.UpdateOn = DateTime.Now;
                    NSession.SaveOrUpdate(stock);
                    NSession.Flush();
                }

            }
            return View();
        }

        [HttpPost]
        public ActionResult Export(string o)
        {
            StringBuilder sb = new StringBuilder();
            string sql = "select  * from Products";
            DataSet ds = new DataSet();
            IDbCommand command = NSession.Connection.CreateCommand();
            command.CommandText = sql;
            SqlDataAdapter da = new SqlDataAdapter(command as SqlCommand);
            da.Fill(ds);
            // 设置编码和附件格式 
            Session["ExportDown"] = ExcelHelper.GetExcelXml(ds);
            return Json(new { IsSuccess = true });
        }

        [HttpPost]
        public JsonResult Create(ProductType obj)
        {
            try
            {
                string filePath = Server.MapPath("~");
                obj.CreateOn = DateTime.Now;
                string pic = obj.PicUrl;
                obj.Status = ProductStatusEnum.销售中.ToString();
                obj.PicUrl = Utilities.BPicPath + obj.SKU + ".jpg";
                obj.SPicUrl = Utilities.SPicPath + obj.SKU + ".png";
                Utilities.DrawImageRectRect(pic, filePath + obj.PicUrl, 310, 310);
                Utilities.DrawImageRectRect(pic, filePath + obj.SPicUrl, 64, 64);
                NSession.SaveOrUpdate(obj);
                NSession.Flush();
                List<ProductComposeType> list1 = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ProductComposeType>>(obj.rows);
                foreach (ProductComposeType productCompose in list1)
                {
                    productCompose.SKU = obj.SKU;
                    productCompose.PId = obj.Id;
                    NSession.Save(productCompose);
                    NSession.Flush();
                }

                IList<WarehouseType> list = NSession.CreateQuery(" from WarehouseType").List<WarehouseType>();

                //
                //在仓库中添加产品库存
                //
                foreach (var item in list)
                {
                    WarehouseStockType stock = new WarehouseStockType();
                    stock.Pic = obj.SPicUrl;
                    stock.WId = item.Id;
                    stock.Warehouse = item.WName;
                    stock.PId = obj.Id;
                    stock.SKU = obj.SKU;
                    stock.Title = obj.ProductName;
                    stock.Qty = 0;
                    stock.UpdateOn = DateTime.Now;
                    NSession.SaveOrUpdate(stock);
                    NSession.Flush();
                }
            }
            catch (Exception ee)
            {
                return Json(new { IsSuccess = false, ErrorMsg = "出错了" });
            }
            return Json(new { IsSuccess = true });
        }

        /// <summary>
        /// 根据Id获取
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public ProductType GetById(int Id)
        {
            ProductType obj = NSession.Get<ProductType>(Id);
            if (obj == null)
            {
                throw new Exception("返回实体为空");
            }
            else
            {
                return obj;
            }
        }

        [OutputCache(Location = OutputCacheLocation.None)]
        public ActionResult Edit(int id)
        {
            ProductType obj = GetById(id);
            return View(obj);
        }

        [HttpPost]
        [OutputCache(Location = OutputCacheLocation.None)]
        public ActionResult Edit(ProductType obj)
        {
            try
            {
                NSession.Update(obj);
                NSession.Flush();
            }
            catch (Exception ee)
            {
                return Json(new { IsSuccess = false, ErrorMsg = "出错了" });
            }
            return Json(new { IsSuccess = true });

        }

        [HttpPost, ActionName("Delete")]
        public JsonResult DeleteConfirmed(int id)
        {
            try
            {
                ProductType obj = GetById(id);
                NSession.Delete(obj);
                NSession.Flush();
            }
            catch (Exception ee)
            {
                return Json(new { IsSuccess = false, ErrorMsg = "出错了" });
            }
            return Json(new { IsSuccess = true });
        }

        [HttpPost, ActionName("DeleteSKU")]
        public JsonResult DeleteConfirmed2(int id)
        {
            try
            {
                SKUCodeType obj = NSession.Get<SKUCodeType>(id);
                NSession.Delete(obj);
                NSession.Flush();
            }
            catch (Exception ee)
            {
                return Json(new { IsSuccess = false, ErrorMsg = "出错了" });
            }
            return Json(new { IsSuccess = true });
        }
        public JsonResult ListQ(string q)
        {
            IList<ProductType> objList = NSession.CreateQuery("from ProductType where SKU like '%" + q + "%'")
                .SetFirstResult(0)
                .SetMaxResults(20)
                .List<ProductType>();

            return Json(new { total = objList.Count, rows = objList });
        }


        public ActionResult SKUScan()
        {
            return View();
        }
        public ActionResult SKUScan2()
        {
            return View();
        }

        public JsonResult SKUCodeList(int page, int rows, string sort, string order, string search)
        {
            string where = "";
            string orderby = " order by Id desc ";
            if (!string.IsNullOrEmpty(sort) && !string.IsNullOrEmpty(order))
            {
                orderby = " order by " + sort + " " + order;
            }
            if (!string.IsNullOrEmpty(search))
            {
                where = Utilities.Resolve(search);
                if (where.Length > 0)
                {
                    where = " where " + where;
                }
            }
            IList<SKUCodeType> objList = NSession.CreateQuery("from SKUCodeType " + where + orderby)
                .SetFirstResult(rows * (page - 1))
                .SetMaxResults(rows)
                .List<SKUCodeType>();
            object count = NSession.CreateQuery("select count(Id) from SKUCodeType " + where).UniqueResult();
            return Json(new { total = count, rows = objList });
        }

        public JsonResult List(int page, int rows, string sort, string order, string search)
        {
            string where = "";
            string orderby = " order by Id desc ";
            if (!string.IsNullOrEmpty(sort) && !string.IsNullOrEmpty(order))
            {
                orderby = " order by " + sort + " " + order;
            }
            if (!string.IsNullOrEmpty(search))
            {
                where = Utilities.Resolve(search);
                if (where.Length > 0)
                {
                    where = " where " + where;
                }
            }
            IList<ProductType> objList = NSession.CreateQuery("from ProductType " + where + orderby)
                .SetFirstResult(rows * (page - 1))
                .SetMaxResults(rows)
                .List<ProductType>();
            object count = NSession.CreateQuery("select count(Id) from ProductType " + where).UniqueResult();
            return Json(new { total = count, rows = objList });
        }

        public JsonResult ZuList(int page, int rows, string sort, string order, string search)
        {
            string where = "";
            string orderby = " order by Id desc ";
            if (!string.IsNullOrEmpty(sort) && !string.IsNullOrEmpty(order))
            {
                orderby = " order by " + sort + " " + order;
            }
            if (!string.IsNullOrEmpty(search))
            {
                where = Utilities.Resolve(search);
                if (where.Length > 0)
                {
                    where = " where " + where;
                }
            }
            IList<ProductComposeType> objList = NSession.CreateQuery("from ProductComposeType " + where + orderby)
                .SetFirstResult(rows * (page - 1))
                .SetMaxResults(rows)
                .List<ProductComposeType>();
            object count = NSession.CreateQuery("select count(Id) from ProductComposeType " + where).UniqueResult();
            return Json(new { total = count, rows = objList });
        }

        public JsonResult HasExist(string sku)
        {
            object count = NSession.CreateQuery("select count(Id) from ProductType where SKU='" + sku + "'").UniqueResult();
            if (Convert.ToInt32(count) > 0)
            {
                return Json(new { IsSuccess = "false" });
            }
            else
            {
                return Json(new { IsSuccess = true });
            }
        }

        public ActionResult SetSKUCode(int code, string sku)
        {
            object count = NSession.CreateQuery("select count(Id) from ProductType where SKU='" + sku + "'").UniqueResult();

            sku = sku.Trim();
            SqlConnection conn = new SqlConnection("server=122.227.207.204;database=Feidu;uid=sa;pwd=`1q2w3e4r");
            string sql = "select top 1 SKU from SkuCode where Code={0}or Code={1} ";
            conn.Open();
            SqlCommand sqlCommand = new SqlCommand(string.Format(sql, code, (code + 1000000)), conn);
            object objSKU = sqlCommand.ExecuteScalar();
            conn.Close();
            if (objSKU != null)
            {
                if (objSKU.ToString().Trim().ToUpper() != sku.Trim().ToUpper())
                {
                    return Json(new { IsSuccess = false, Result = "这个条码对应是的" + objSKU + ",不是现在的：" + sku + "！" });
                }
            }

            if (Convert.ToInt32(count) > 0)
            {
                object count1 =
                    NSession.CreateQuery("select count(Id) from SKUCodeType where Code=:p").SetInt32("p", code).
                        UniqueResult();
                if (Convert.ToInt32(count1) == 0)
                {
                    SKUCodeType skuCode = new SKUCodeType { Code = code, SKU = sku, IsNew = 0, IsOut = 0 };
                    NSession.Save(skuCode);
                    NSession.Flush();
                    return Json(new { IsSuccess = true, Result = "添加成功！" });
                }
                else
                {
                    return Json(new { IsSuccess = false, Result = "这个条码已经添加！" });
                }
            }
            else
            {
                return Json(new { IsSuccess = false, Result = "没有这个产品！" });
            }
        }

        public ActionResult SetSKUCode2(int code)
        {
            IList<SKUCodeType> list =
                 NSession.CreateQuery("from SKUCodeType where Code=:p").SetInt32("p", code).SetMaxResults(1).List
                     <SKUCodeType>();
            if (list.Count > 0)
            {
                SKUCodeType sku = list[0];
                if (sku.IsOut == 1 || sku.IsSend == 1)
                {
                    return Json(new { IsSuccess = false, Result = "条码：" + code + " 已经配过货,SKU:" + sku.SKU + " 出库时间：" + sku.PeiOn + ",出库订单:" + sku.OrderNo + " ,请将此产品单独挑出来！" });
                }
                if (sku.IsScan == 1)
                {
                    return Json(new { IsSuccess = false, Result = "条码：" + code + " 已经清点扫描了,SKU:" + sku.SKU + " 刚刚已经扫描过了。你查看下是条码重复扫描了，还是有贴重复的了！" });
                }
                sku.IsScan = 1;
                NSession.Save(sku);
                NSession.Flush();
                object obj =
                    NSession.CreateQuery("select count(Id) from SKUCodeType where SKU=:p and IsScan=1 and IsOut=0").SetString("p", sku.SKU).
                        UniqueResult();
                return Json(new { IsSuccess = true, Result = "条码：" + code + " 的信息.SKU：" + sku.SKU + " 此条码未出库。条码正确！！！", ccc = sku.SKU + "已经扫描了" + obj + "个" });
            }
            else
            {
                return Json(new { IsSuccess = false, Result = "条码：" + code + " 无法找到 ,请查看扫描是否正确！" });
            }
        }


        public ActionResult GetSKUByCode(string code)
        {
            IList<SKUCodeType> list =
                 NSession.CreateQuery("from SKUCodeType where Code=:p").SetString("p", code).SetMaxResults(1).List
                     <SKUCodeType>();
            if (list.Count > 0)
            {
                SKUCodeType sku = list[0];
                if (sku.IsOut == 0)
                {
                    return Json(new { IsSuccess = true, Result = sku.SKU.Trim() });
                }
                else
                {
                    return Json(new { IsSuccess = false, Result = "当前产品已经出库过了！" });
                }
            }
            return Json(new { IsSuccess = false, Result = "没有找到这个产品！" });

        }

        public JsonResult SearchSKU(string id) 
        {
            IList<ProductType> obj = NSession.CreateQuery("from ProductType where SKU='"+id+"'").List<ProductType>();
            return Json(obj,JsonRequestBehavior.AllowGet);
        }
        public JsonResult Freight(decimal price, decimal weight, int qty, decimal onlineprice, string Currency, string LogisticMode, int Country)
<<<<<<< HEAD
        {
            decimal freight = decimal.Parse((GetFreight(weight, LogisticMode, Country)).ToString("f6"));
            decimal currency =decimal.Parse(Math.Round(GetCurrency(Currency),2).ToString());
            decimal profit =(onlineprice * currency - price - freight) * qty;
            return Json(profit, JsonRequestBehavior.AllowGet);
        }
        private decimal GetFreight(decimal weight, string logisticMode, int country)
        {
            decimal discount = 0;
            decimal ReturnFreight = 0;
            IList<LogisticsModeType> logmode = NSession.CreateQuery("from LogisticsModeType where LogisticsCode='"+logisticMode+"'").List<LogisticsModeType>();
            foreach(var item in logmode)
            {
                discount =decimal.Parse(item.Discount.ToString());
                IList<LogisticsAreaType> area = NSession.CreateQuery("from LogisticsAreaType where LId='"+item.ParentID+"'").List<LogisticsAreaType>();
                foreach (var it in area) 
                {
                    object obj=NSession.CreateQuery("select count(Id) from LogisticsAreaCountryType where CountryCode='" + country + "' and AreaCode='"+it.Id+"'").UniqueResult();
                    if(Convert.ToInt32(obj)>0)
                    {
                        IList<LogisticsFreightType> list = NSession.CreateQuery("from LogisticsFreightType where AReaCode='"+it.Id+"'").List<LogisticsFreightType>();
                        foreach (var fre in list)
                        {
                            if (fre.EveryFee != 0)
                            {
                                ReturnFreight =weight *decimal.Parse(fre.EveryFee.ToString()) +decimal.Parse(fre.ProcessingFee.ToString()) *decimal.Parse( discount.ToString());
                            }
                            else
                            {
                                ReturnFreight =decimal.Parse(fre.FristFreight.ToString()) + decimal.Parse(fre.ProcessingFee.ToString()) + (weight - decimal.Parse(fre.FristWeight.ToString())) / decimal.Parse(fre.IncrementWeight.ToString()) * decimal.Parse(fre.IncrementFreight.ToString());
                            }
                        }
                    }
                }
            }

            return ReturnFreight; 
        }
        public double GetCurrency(string code)
        {
            double curr = 0;
              IList<CurrencyType> list = NSession.CreateQuery("from CurrencyType where CurrencyCode='"+code+"'").List<CurrencyType>();
            foreach(var s in list)
            {
                curr =s.CurrencyValue;
            }
            return curr;
=======
        {
            decimal freight = decimal.Parse((GetFreight(weight*qty, LogisticMode, Country)).ToString("f6"));
            if(freight==-1)
                return Json("cz", JsonRequestBehavior.AllowGet);
            decimal currency =decimal.Parse(Math.Round(GetCurrency(Currency),2).ToString());
            decimal profit =(onlineprice * currency - price) * qty - freight;
            return Json(profit, JsonRequestBehavior.AllowGet);
        }
        private decimal GetFreight(decimal weight, string logisticMode, int country)
        {
            decimal discount = 0;
            decimal ReturnFreight = 0;
            IList<LogisticsModeType> logmode = NSession.CreateQuery("from LogisticsModeType where LogisticsCode='"+logisticMode+"'").List<LogisticsModeType>();
            foreach(var item in logmode)
            {
                discount =decimal.Parse(item.Discount.ToString());
                IList<LogisticsAreaType> area = NSession.CreateQuery("from LogisticsAreaType where LId='"+item.ParentID+"'").List<LogisticsAreaType>();
                foreach (var it in area) 
                {
                    object obj=NSession.CreateQuery("select count(Id) from LogisticsAreaCountryType where CountryCode='" + country + "' and AreaCode='"+it.Id+"'").UniqueResult();
                    if(Convert.ToInt32(obj)>0)
                    {
                        IList<LogisticsFreightType> list = NSession.CreateQuery("from LogisticsFreightType where AReaCode='"+it.Id+"'").List<LogisticsFreightType>();
                        foreach (var fre in list)
                        {
                            if (weight <= decimal.Parse(fre.EndWeight.ToString()))
                            {
                                if (fre.EveryFee != 0)
                                {
                                    ReturnFreight = weight * decimal.Parse(fre.EveryFee.ToString()) + decimal.Parse(fre.ProcessingFee.ToString()) * decimal.Parse(discount.ToString());
                                }
                                else
                                {
                                    ReturnFreight = decimal.Parse(fre.FristFreight.ToString()) + decimal.Parse(fre.ProcessingFee.ToString()) + (weight - decimal.Parse(fre.FristWeight.ToString())) / decimal.Parse(fre.IncrementWeight.ToString()) * decimal.Parse(fre.IncrementFreight.ToString());
                                }
                            }
                            else
                            {
                                return -1;
                            }
                        }
                    }
                }
            }

            return ReturnFreight; 
        }
        public double GetCurrency(string code)
        {
            double curr = 0;
              IList<CurrencyType> list = NSession.CreateQuery("from CurrencyType where CurrencyCode='"+code+"'").List<CurrencyType>();
            foreach(var s in list)
            {
                curr =s.CurrencyValue;
            }
            return curr;
>>>>>>> kewei/ttt
        }
    }
}

