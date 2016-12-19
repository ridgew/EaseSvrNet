using System;
using Gwsoft.SharpOrm;
using Gwsoft.SharpOrm.Config;
using System.Collections;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Gwsoft.Ease.Model.Stat
{
    [Serializable]
    [BindTable("DefaultDb", "Stat_Chart_Rules")]
    public class StatChartRules : TableEntry, IEntryExtension<StatChartRules>
    {
        /// <summary>
        /// 自动编号
        /// </summary>
        [PrimaryKey]
        [Identity]
        public long Id { get; set; }

        /// <summary>
        /// 所属站点编号
        /// </summary>
        [MaxLength(15)]
        [Comment("所属站点编号")]
        public string Siteid { get; set; }

        /// <summary>
        /// 统计项目名称
        /// </summary>
        [MaxLength(200)]
        [Comment("统计项目名称")]
        public string Title { get; set; }

        /// <summary>
        /// 报表X轴名称
        /// </summary>
        [MaxLength(50)]
        [Comment("报表X轴名称")]
        public string Xname { get; set; }

        /// <summary>
        /// 报表Y轴名称
        /// </summary>
        [MaxLength(50)]
        [Comment("报表Y轴名称")]
        public string Yname { get; set; }

        /// <summary>
        /// 报表X轴规则
        /// </summary>
        [MaxLength(50)]
        [Comment("报表X轴规则")]
        public string XFormat { get; set; }

        /// <summary>
        /// 报表Y轴规则
        /// </summary>
        [MaxLength(50)]
        [Comment("报表Y轴规则")]
        public string YFormat { get; set; }

        /// <summary>
        /// 数据集显示名称
        /// </summary>
        [MaxLength(2000)]
        [Comment("数据集显示名称")]
        public string Tablenamelist { get; set; }

        /// <summary>
        /// X轴是否均匀分布，0为否，1为是
        /// </summary>
        [MaxLength(2000)]
        [Comment("X轴是否均匀分布，0为否，1为是")]
        public bool XValueIndexed { get; set; }

        /// <summary>
        /// 统计信息代码内容
        /// </summary>
        [SqlDbType("ntext")]
        [Comment("统计信息代码内容")]
        public string Sqltext { get; set; }

        /// <summary>
        /// 代码执行方式，0为普通文本，1为存储过程
        /// </summary>
        [Comment("代码执行方式，0为普通文本，1为存储过程")]
        public bool IsProcedure { get; set; }

        /// <summary>
        /// 参数列表
        /// </summary>
        [MaxLength(4000)]
        [Comment("参数列表")]
        public string ParanameList { get; set; }

        /// <summary>
        /// 参数值列表
        /// </summary>
        [SqlDbType("ntext")]
        [Comment("参数值列表")]
        public string ParavalueList { get; set; }

        /// <summary>
        /// 报表添加时间
        /// </summary>
        [Comment("报表添加时间")]
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 统计类型：0数据列表，1图形报表
        /// </summary>
        public byte StType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public short StDataType { get; set; }

        /// <summary>
        /// 分类编号:所属栏目
        /// </summary>
        [SqlDbType("vchar(10)")]
        public string Classid { get; set; }


        #region IEntryExtension<StatChartRules> 成员

        /// <summary>
        /// 获取相关扩展数据(非自身属性)
        /// </summary>
        /// <returns></returns>
        public DictionaryEntry[] GetExtenEntryArray()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 移除相关扩展数据实现
        /// </summary>
        public void RemoveExtension()
        {
            
        }

        /// <summary>
        /// 参数项值枚举
        /// </summary>
        private enum ParamItem
        {
            ParamName = 0,
            ParamType = 1,
            ParamVal = 2,
            ParamMemo = 3
        }

        /// <summary>
        /// 从包含扩展数据的键值词典库设置实例类型和处理扩展数据
        /// </summary>
        /// <param name="entryArray">词典键值对中包含自身属性，
        /// 参见<c>OnEntryMissingProperty</c>委托和从扩展数据绑定实体方法。</param>
        /// <returns></returns>
        public StatChartRules SetWithExtension(DictionaryEntry[] entryArray)
        {
            List<string> pList = new List<string>();
            Dictionary<string, string> ParamDict = new Dictionary<string, string>();

            Regex itemRe = new Regex("^tpt(\\w+)(\\d+)$", RegexOptions.IgnoreCase);
            //tptParamName1,tptParamType1,tptParamVal1,tptParamMemo1
            StatChartRules rule = new StatChartRules().DataBind<StatChartRules>(entryArray,
                (name, value) => {
                    Match m = itemRe.Match(name);
                    if (m.Success)
                    {
                        string key = m.Groups[2].Value;
                        if (!ParamDict.ContainsKey(key))
                        {
                            ParamDict.Add(key, " | | | ");
                        }

                        string[] itemArr = ParamDict[key].Split('|');
                        try
                        {
                            ParamItem Item = (ParamItem)Enum.Parse(typeof(ParamItem), m.Groups[1].Value);
                            itemArr[Item.GetHashCode()] = value.ToString();
                        }
                        catch (Exception) { 
                            
                        }
                        ParamDict[key] = string.Join("|", itemArr);
                    }
                });

            foreach (string key in ParamDict.Keys)
            {
                pList.Add(ParamDict[key]);
            }

            //ParanameList "tDayStart|2|20090401|开始日期$tDayEnd|2|20090531|结束日期"
            rule.ParanameList = (pList.Count == 0) ? "" : string.Join("$", pList.ToArray());
            rule.Siteid = "qazxswedcvfrtgb";
            rule.Classid = "DC8YHL8H63";
            rule.StDataType = 0;
            rule.StType = 1;

            if (rule.Id == 0)
            {
                rule.Id = Convert.ToInt64(rule.Insert(true));
            }
            else
            {
                rule.Update(new string[] { "IsProcedure", "StType", "StDataType" });
                //Gwsoft.SharpOrm.Util.Common.Log("log", sw => {
                //    sw.WriteLine(Gwsoft.SharpOrm.ExtensionUtil.ToJSON(rule));
                //});
            }

            return rule;
        }

        #endregion
    }
}