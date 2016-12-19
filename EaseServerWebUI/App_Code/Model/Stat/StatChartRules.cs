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
        /// �Զ����
        /// </summary>
        [PrimaryKey]
        [Identity]
        public long Id { get; set; }

        /// <summary>
        /// ����վ����
        /// </summary>
        [MaxLength(15)]
        [Comment("����վ����")]
        public string Siteid { get; set; }

        /// <summary>
        /// ͳ����Ŀ����
        /// </summary>
        [MaxLength(200)]
        [Comment("ͳ����Ŀ����")]
        public string Title { get; set; }

        /// <summary>
        /// ����X������
        /// </summary>
        [MaxLength(50)]
        [Comment("����X������")]
        public string Xname { get; set; }

        /// <summary>
        /// ����Y������
        /// </summary>
        [MaxLength(50)]
        [Comment("����Y������")]
        public string Yname { get; set; }

        /// <summary>
        /// ����X�����
        /// </summary>
        [MaxLength(50)]
        [Comment("����X�����")]
        public string XFormat { get; set; }

        /// <summary>
        /// ����Y�����
        /// </summary>
        [MaxLength(50)]
        [Comment("����Y�����")]
        public string YFormat { get; set; }

        /// <summary>
        /// ���ݼ���ʾ����
        /// </summary>
        [MaxLength(2000)]
        [Comment("���ݼ���ʾ����")]
        public string Tablenamelist { get; set; }

        /// <summary>
        /// X���Ƿ���ȷֲ���0Ϊ��1Ϊ��
        /// </summary>
        [MaxLength(2000)]
        [Comment("X���Ƿ���ȷֲ���0Ϊ��1Ϊ��")]
        public bool XValueIndexed { get; set; }

        /// <summary>
        /// ͳ����Ϣ��������
        /// </summary>
        [SqlDbType("ntext")]
        [Comment("ͳ����Ϣ��������")]
        public string Sqltext { get; set; }

        /// <summary>
        /// ����ִ�з�ʽ��0Ϊ��ͨ�ı���1Ϊ�洢����
        /// </summary>
        [Comment("����ִ�з�ʽ��0Ϊ��ͨ�ı���1Ϊ�洢����")]
        public bool IsProcedure { get; set; }

        /// <summary>
        /// �����б�
        /// </summary>
        [MaxLength(4000)]
        [Comment("�����б�")]
        public string ParanameList { get; set; }

        /// <summary>
        /// ����ֵ�б�
        /// </summary>
        [SqlDbType("ntext")]
        [Comment("����ֵ�б�")]
        public string ParavalueList { get; set; }

        /// <summary>
        /// �������ʱ��
        /// </summary>
        [Comment("�������ʱ��")]
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// ͳ�����ͣ�0�����б�1ͼ�α���
        /// </summary>
        public byte StType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public short StDataType { get; set; }

        /// <summary>
        /// ������:������Ŀ
        /// </summary>
        [SqlDbType("vchar(10)")]
        public string Classid { get; set; }


        #region IEntryExtension<StatChartRules> ��Ա

        /// <summary>
        /// ��ȡ�����չ����(����������)
        /// </summary>
        /// <returns></returns>
        public DictionaryEntry[] GetExtenEntryArray()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// �Ƴ������չ����ʵ��
        /// </summary>
        public void RemoveExtension()
        {
            
        }

        /// <summary>
        /// ������ֵö��
        /// </summary>
        private enum ParamItem
        {
            ParamName = 0,
            ParamType = 1,
            ParamVal = 2,
            ParamMemo = 3
        }

        /// <summary>
        /// �Ӱ�����չ���ݵļ�ֵ�ʵ������ʵ�����ͺʹ�����չ����
        /// </summary>
        /// <param name="entryArray">�ʵ��ֵ���а����������ԣ�
        /// �μ�<c>OnEntryMissingProperty</c>ί�кʹ���չ���ݰ�ʵ�巽����</param>
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

            //ParanameList "tDayStart|2|20090401|��ʼ����$tDayEnd|2|20090531|��������"
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