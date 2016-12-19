//EASE系统公用JS脚本
//检测输入，更多的判断自行添加
function CheckInput(Obj, Type) {
	var _val = Obj.value;
	if (_val != "") {
		switch (Type) {
			//纯数字
			case 1:
				Obj.value = _val.replace(/[^0-9]*/gi, '');
				break;
			//纯字母
			case 2:
				Obj.value = _val.replace(/[^a-zA-Z]*/gi, '');
				break;
			//字母加下划线
			case 3:
				Obj.value = _val.replace(/[^a-zA-Z_]*/gi, '');
				break;
			//字母加数字
			case 4:
				Obj.value = _val.replace(/[^a-zA-Z0-9]*/gi, '');
				break;
			default:
				//
				break;
		}
	}
}

//获取页面传递参数
function getPageParamVal(Page, paramName) {
	var _url = Page.location.search.replace("?", "");
	var _return = "";
	if (_url.indexOf(paramName + "=") != -1)
	{
		var _tArr = _url.split("&");
		for (var i = 0; i < _tArr.length; i ++)
		{
			if (_tArr[i].indexOf("=") != -1)
			{
				if (_tArr[i].split("=")[0] == paramName)
				{
					_return = _tArr[i].split("=")[1];
					break;
				}
			}
		}
	}
	if (_return != "")
		_return = unescape(_return);
	return _return;
}


//插入关键字
function InsertKeys(obj,keys)
{
	var _val = obj.value;
	if (_val == "")
	{
		obj.value = keys;
	}
	else
	{
		var _kArr = _val.split(" ");
		if (!StrIndexOfArrTF(keys,_kArr))
			obj.value = _val + " " + keys;
	}
}

//获取字符串是否包含于数组
function StrIndexOfArrTF(Str,Arr)
{
	var _TF = false;
	for (var i = 0; i < Arr.length; i ++)
	{
		if (Str == Arr[i])
		{
			_TF = true;
			break;
		}
	}
	return _TF;
}


//获取当前时间
function getNow()
{
	var d = new Date();
	return d.getFullYear() + "-" + (d.getMonth() + 1) + "-" + d.getDate() + " " + d.getHours() + ":" + d.getMinutes() + ":" + d.getSeconds();
};


//常用的验证//需要的可以自己扩展
function Validator(Str,Type)
{
	var RegEx;
	switch (Type)
	{
		//整数
		case 1:
			RegEx = /^-?[0-9]\d*$/;
			break;
		//浮点数
		case 2:
			RegEx = /^-?([0-9]\d*\.\d*|0\.\d*[0-9]\d*|0?\.0+|0)$/;
			break;
		//日期
		case 3:
			RegEx = /^[0-9]{4}-[0-1]{1}[0-9]{1}-[0-3]{1}[0-9]{1}( [0-2]{1}[0-9]{1}:[0-5]{1}[0-9]{1}:[0-5]{1}[0-9]{1})?$/;
			break;
		default:
			//
			break;
	}
	return RegEx.test(Str);
}

//资源数据添加部分根据请求返回结果组织扩展字段内容
function GetExtFieldInfoForResDataForm(Json)
{
	//单独字段的JSON对象
	var _fInfo = Json; 
	//var _InnerInfo = "    	        <tr>";
	//_InnerInfo += "    				<td class=\"td01\" valign=\"middle\" style=\"width:10%;\">" + _fInfo.ExtFieldCName + "：</td>";
	//_InnerInfo += "    				<td class=\"td02\" valign=\"middle\" style=\"width:90%;\">";
	var _InnerInfo = "";
	//字段默认值
	var _dfVal = _fInfo.ExtFieldDFValue;
	if (_dfVal == "" || _dfVal == "''" || _dfVal == "\u0027\u0027")
		_dfVal = "";
	if (_dfVal.toLowerCase() == "getdate()")
		_dfVal = top.getNow();
	//输入方式判断
	switch (_fInfo.ExtFieldInputType)
	{
		//单行文本
		case 1 :
			//最大长度
			var _max = "";
			if (!isNaN(_fInfo.ExtFieldInputLens))
			{
				if (parseInt(_fInfo.ExtFieldInputLens) > 0)
					_max = " maxlength=\"" + parseInt(_fInfo.ExtFieldInputLens) + "\"";
			}
			_InnerInfo += "<input class=\"inp05\" type=\"text\" name=\"" + _fInfo.ExtFieldEName + "\" id=\"" + _fInfo.ExtFieldEName + "\" value=\"" + _dfVal + "\" " + _max + " title=\"" + _fInfo.ExtFieldInfo + "\" />";
			//是否允许为空
			break;
		//多行文本
		case 2 :
			_InnerInfo += "<textarea name=\"" + _fInfo.ExtFieldEName + "\" id=\"" + _fInfo.ExtFieldEName + "\" style=\"width:660px; height:100px; padding-top:8px;\" rows=\"3\" cols=\"30\">" + _dfVal + "</textarea>";
			break;
		//日期选择器
		case 3 :
			_InnerInfo += "<input class=\"inp01\" style=\"width:200px;\" type=\"text\" name=\"" + _fInfo.ExtFieldEName + "\" id=\"" + _fInfo.ExtFieldEName + "\" value=\"" + _dfVal + "\" readonly=\"readonly\" maxlength=\"20\" title=\"" + _fInfo.ExtFieldInfo + "，点击选择日期按钮进行选择！\" onclick=\"javascript:datepicker({datedom:'" + _fInfo.ExtFieldEName + "',dateHMS:true},event);\" />"; 
			_InnerInfo += "<input type=\"button\" class=\"Picbtn\" style=\"margin-top:1px;\" name=\"btn_SelDate\" value=\"选择日期\" onclick=\"javascript:datepicker({datedom:'" + _fInfo.ExtFieldEName + "',dateHMS:true},event);\" />";
			break;
		//文件选择器
		case 4:
			_InnerInfo += "<input class=\"inp01\"  style=\"width:200px;\" type=\"text\" name=\"" + _fInfo.ExtFieldEName + "\" id=\"" + _fInfo.ExtFieldEName + "\" value=\"" + _dfVal + "\" readonly=\"readonly\" maxlength=\"250\" title=\"" + _fInfo.ExtFieldInfo + "，点击选择文件按钮选择文件\" onclick=\"javascript:selectFile(this);\" />";
			_InnerInfo += "<input type=\"button\" class=\"Picbtn\" style=\"margin-top:1px;\" name=\"btn_SelFile\" value=\"选择文件\" onclick=\"javascript:selectFile(document.getElementById('" + _fInfo.ExtFieldEName + "'));\" />";		
			break;
		//单选
		case 5 :
			if(_fInfo.SelectDataValue != null)
			{
				for (var i = 0; i < _fInfo.SelectDataValue.length; i ++)
				{
					var _dvArr = _fInfo.SelectDataValue[i].split("|");
					_InnerInfo += "<input type=\"radio\" name=\"" + _fInfo.ExtFieldEName + "\" id=\"" + _fInfo.ExtFieldEName + "_" + i + "\" value=\"" + _dvArr[1] + "\" />" + _dvArr[0] + "\n";
				}
			}
			break;
		//复选
		case 6 :
			if(_fInfo.SelectDataValue != null)
			{
				for (var i = 0; i < _fInfo.SelectDataValue.length; i ++)
				{
					var _dvArr = _fInfo.SelectDataValue[i].split("|");
					_InnerInfo += "<input type=\"checkbox\" name=\"" + _fInfo.ExtFieldEName + "\" id=\"" + _fInfo.ExtFieldEName + "_" + i + "\" value=\"" + _dvArr[1] + "\" />" + _dvArr[0] + "\n";
				}
			}
			break;
		case 7 :
			_InnerInfo += "<select class=\"sel01\" name=\"" + _fInfo.ExtFieldEName + "\" id=\"" + _fInfo.ExtFieldEName + "\">\n";
			if(_fInfo.SelectDataValue != null)
			{
				for (var i = 0; i < _fInfo.SelectDataValue.length; i ++)
				{
					var _dvArr = _fInfo.SelectDataValue[i].split("|");
					_InnerInfo += "<option value=\"" + _dvArr[1] + "\">" + _dvArr[0] + "</option>\n";
				}
			}
			_InnerInfo += "</select>";
			break;
	}
	//if (!_fInfo.ExtFiedlIsNullTF)
		//_InnerInfo += "<font color=\"#FF0000\">*</font>";
	//_InnerInfo += "<font>" + _fInfo.ExtFieldInfo + "</font>"
	//_InnerInfo += "    				</td>";
	//_InnerInfo += "    	        </tr>";
	
	//
	return _InnerInfo;
}

//获取当前时间
function getNow_Json() {
    var _d = new Date();
    return { YY: _d.getFullYear(), MM: getTwotime(_d.getMonth() + 1), DD: getTwotime(_d.getDate()), HH: getTwotime(_d.getHours()), MI: getTwotime(_d.getMinutes()), SS: getTwotime(_d.getSeconds()) };
}

//一位时间数组前补零
function getTwotime(t) {
    if (t.toString().length == 1)
        t = "0" + t.toString();
    return t;
}


function rnd() {
    rnd.seed = (rnd.seed * 9301 + 49297) % 233280;
    return rnd.seed / (233280.0);
}

function rand(number) {
    rnd.today = new Date();
    rnd.seed = rnd.today.getTime();
    return Math.ceil(rnd() * number);
}

function GetRand() {
    var _rnd = rand(100000);
    var _t = parseInt(parseInt(getNow_Json().YY) + parseInt(getNow_Json().MM) + parseInt(getNow_Json().DD) + parseInt(getNow_Json().HH) + parseInt(getNow_Json().MI) + parseInt(getNow_Json().SS));
    return (_t.toString() + _rnd.toString());
}