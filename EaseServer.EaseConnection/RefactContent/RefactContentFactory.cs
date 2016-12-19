using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EaseServer.EaseConnection.RefactContent
{
    /// <summary>
    /// 重新编辑Ease标签内容处理封装
    /// </summary>
    public class RefactContentFactory
    {
        /// <summary>
        /// Initializes the <see cref="RefactContentFactory"/> class.
        /// </summary>
        private RefactContentFactory()
        {
        }

        private static EaseCodeFilter _tCopositeHandler = null;
        /// <summary>
        /// 获取Ease标签内容处理委托设置;在处理过程中发生的异常将被隐藏。
        /// </summary>
        /// <value>Ease标签内容处理委托</value>
        public static EaseCodeFilter CompositeHandler
        {
            get
            {
                if (_tCopositeHandler == null)
                {
                    _tCopositeHandler = (bid, content, isHtml) =>
                    {
                        #region 根据配置设置
                        foreach (RefactContentStep step in RefactContentConfig.Instance.RefactContentSteps)
                        {
                            if (step.OnlyForBusinessIds != null)
                            {
                                string[] setArr = step.OnlyForBusinessIds.Split(new char[] { ',', '|', ';' }, StringSplitOptions.RemoveEmptyEntries);
                                if (Array.IndexOf<string>(setArr, bid.ToString()) == -1)
                                    continue;
                            }

                            try
                            {
                                content = step.RefactContent(bid, content, isHtml);
                                //System.Diagnostics.Trace.Write("\n*" + step.MethodName + "已处理成如下内容：\n");
                                //System.Diagnostics.Trace.Write(content+ "\n");
                                //System.Diagnostics.Trace.Write("-------------------------------\n");
                            }
                            catch (Exception refactEx)
                            {
                                System.Diagnostics.Trace.WriteLine(string.Format("* 类型{0}中的方法{1}处理异常:{2}",
                                    step.EditTypeName, step.MethodName, refactEx));
                            }
                        }
                        return content;
                        #endregion
                    };
                }
                return _tCopositeHandler;
            }
        }



    }
}
