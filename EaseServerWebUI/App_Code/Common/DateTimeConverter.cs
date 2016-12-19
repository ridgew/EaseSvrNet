using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Web.Script.Serialization;

namespace Gwsoft.Ease.Common
{
    /// <summary>
    ///DateTime数据类型序列化类
    /// </summary>
    public class DateTimeConverter : JavaScriptConverter
    {
        private ReadOnlyCollection<Type> _supportedTypes = new ReadOnlyCollection<Type>(new Type[] { typeof(DateTime) });

        /// <summary>
        /// 当在派生类中重写时，将所提供的字典转换为指定类型的对象。
        /// </summary>
        /// <param name="dictionary">作为名称/值对存储的属性数据的 <see cref="T:System.Collections.Generic.IDictionary`2"/> 实例。</param>
        /// <param name="type">所生成对象的类型。</param>
        /// <param name="serializer"><see cref="T:System.Web.Script.Serialization.JavaScriptSerializer"/> 实例。</param>
        /// <returns>反序列化的对象。</returns>
        public override object Deserialize(System.Collections.Generic.IDictionary<string, object> dictionary, Type type, JavaScriptSerializer serializer)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// 当在派生类中重写时，生成名称/值对的字典。
        /// </summary>
        /// <param name="obj">要序列化的对象。</param>
        /// <param name="serializer">负责序列化的对象。</param>
        /// <returns>一个对象，包含表示该对象数据的键/值对。</returns>
        public override System.Collections.Generic.IDictionary<string, object> Serialize(object obj, JavaScriptSerializer serializer)
        {
            IDictionary<string, object> dictionary = new Dictionary<string, object>();
            bool exists = obj.Equals(DateTime.MinValue) ? false : true;
            dictionary["Value"] = exists ? ((DateTime)obj).ToString("yyyy-MM-dd HH:mm:ss") : "";
            return dictionary;
        }

        /// <summary>
        /// 当在派生类中重写时，获取受支持类型的集合。
        /// </summary>
        /// <value></value>
        /// <returns>一个实现 <see cref="T:System.Collections.Generic.IEnumerable`1"/> 的对象，用于表示转换器支持的类型。</returns>
        public override System.Collections.Generic.IEnumerable<Type> SupportedTypes
        {
            get
            {
                return this._supportedTypes;
            }
        }
    }
}