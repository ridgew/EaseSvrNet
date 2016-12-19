function DateDemo() {
	var d, s;
	s = "";
	d = new Date();
	s += (d.getYear() < 1000 ? 1900 + d.getYear() : d.getYear()) + "-";
	s += toCode_Sys((d.getMonth() + 1), 2) + "-";
	s += toCode_Sys(d.getDate(), 2);
	return (s);
}

function DateDemoTime(){
	var d, s;
	s='';
	d = new Date();
	s += (d.getYear()<1000?1900+d.getYear():d.getYear())+"-";
	s += toCode_Sys((d.getMonth() + 1),2)+"-";
	s += toCode_Sys(d.getDate(),2);
   s +=" 00:00:00";
	return(s);
}

function toCode_Sys(str,len,addchar) {
	str = str.toString();
	len = (len == null) ? 4 : len;
	var s = "";
	for (var i=0;i<(len - str.length);i++) {
		if ( addchar== null ) s += "0";
		else  s +=addchar;
	}
	return(s + str);
}
  //得到昨天日期:"yyyy-mm-dd"
  function yesterDateDemo(format){
   var monthDays = new Array(31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31);
   var d, s, y, m;
   d = new Date();
   y = d.getYear();
   m = d.getMonth();
   if (((y % 4 == 0) && (y % 100 != 0)) || (y % 400 == 0)){monthDays[1] = 29;}
   if(m == 0 && d.getDate()== 1){
     s += (y - 1)+"-12-";
     s += monthDays[11];
   }else if (d.getDate()== 1){
     s += y+"-";
     s += m+"-";
     s += monthDays[m-1];
   }else{
     s += y+"-";
     s += (m + 1) + "-";
     s += d.getDate()-1;
   }
   if(format == 1){
     s=s.substring(9,30);
   }else{
     s=s.substring(9,21);
   }
   //S=D;
   return(s);
  }
  
 function fnGerTime(time) {

  var StrDate=time.value;
  //alert(StrDate);
  var i= StrDate.indexOf("-");
  var Year= StrDate.substring(0,i);
  var j= StrDate.indexOf("-",i+1);
  var Month= StrDate.substring(i+1,j)*1;
      //Month=parseInt(Month)-1;


  //i=StrDate.indexOf(" ");
  var Day= StrDate.substring(j+1);


  if((Month>0)&&(Month<=12)&&(Day>0)&&(Year>0)){
  if (Month==4|| Month==6 || Month==9 || Month==11){
       if (Day>30){
         return(-1);
       }
     }
    if (Month==1||Month==3|| Month==5 ||Month==7||Month==8|| Month==10||Month==12){
       if (Day>31){
         return(-1);
       }
     }
    if (Month==2){
       if (Day>29){
         return(-1);
       }
     }
   }
  else{
    return(-1);
  }
  time.value=Year+"-"+toCode_Sys(Month,2)+"-"+toCode_Sys(Day,2);
  return(1);
 }
 
  function CompareTime(time1,time2){
   var parse1,parse2,parse0;
   var days,times;
   var year,month,day,hours,minute,second

   var date1,date2;
   var strs;
   var strs1;

   strs=time1.split(" ");
   //alert(strs.length);
   days=strs[0];
   strs1=days.split("-");
   year=strs1[0];
   month=strs1[1];
   day=strs1[2];
   date1=month+"/"+day+"/"+year;

   if(strs.length>1){
    times=strs[1];
    date1=date1+" "+times;
   }
   parse1=Date.parse(date1);

   strs=time2.split(" ");
   days=strs[0];
   strs1=days.split("-");
   year=strs1[0];
   month=strs1[1];
   day=strs1[2];
   date2=month+"/"+day+"/"+year;
   if(strs.length>1){
    times=strs[1];
    date2=date2+" "+times;
   }
   parse2=Date.parse(date2);
   parse0=parse2-parse1;
   return(parse0);
  }

//*****共用涵数，以下方法在IE和火弧上正常运行********************************************************
//********************************************************************
//检查JSON合法性
function errCheck(resp){
  var json=resp.evalJSON();

  if(json==null){
    alert('未知错误！');
	return false;
  }
  if(json.err==2){  //登陆
    alert(json.errinfo);
  //  parent.location.href='../index.jsp?clear=1';
    return false;
  }
  if(json.err==1){
    alert(json.errinfo);
    return false;
  }
  return true;
}



//************************************************************************
//替换字符特殊字符
function fnStrReplace_str(re_str) {
  var re = /</g;
  var re1=/>/g;
  var re2=/&/g;
  var re3=/\'/g;
  var re4=/\$/g;
  var re6=/\"/g;
  var values=re_str;

  values=values.replace(re2,"&amp;");
  values=values.replace(re,"&lt;");
  values=values.replace(re1,"&gt;");
  values=values.replace(re3,"&apos;&apos;");
  values=values.replace(re6,"&quot;");

  values=values.replace(/(^\s*)|(\s*$)/g, "");
  re_str=values;



  return re_str;


}

//************************************************
  //***
  //  显示下拉菜单
  //*/
  function showSelectByJson(resp,objSel){
           //alert("o");
           //alert(resp);
           var results = resp.evalJSON();
           //alert(results.length);
           for (i=results.length; i>0; i--) {
              thisResult = results[i-1];
              fnAddSelOptionBlank_Sys(objSel,thisResult.CLASS_ID,thisResult.CLASS_NAME);
           }
   }
//************************************************

//加入"select"对象选择项
function fnAddSelOptionBlank_Sys(objSel,strValue,strText) {
		var oOption = document.createElement("OPTION");
		oOption.value = strValue;
		oOption.text = strText;
		objSel.options.add(oOption,0);
		objSel.value = strValue;
}


  //清楚选择框
  function fnRemoveOption(REMOVESEL) {
	  for (var i=REMOVESEL.options.length-1;i>=0;i--) {
			REMOVESEL.remove(i);
	  }

  }


//***************************************************
//清除所有的输入项,包括"INPUT"和"TEXTAREA"对象
function fnClearAllInfo_Sys(checkele) {

	        var ele;
	        if (checkele == null){
                  ele = document;

                }
                else{
                  ele=checkele;
                }

		var item;
		var obj=ele.getElementsByTagName("INPUT");
		//var obj = ele.all.tags("INPUT");
		for (i = 0; i < obj.length; i++) {
			item = obj[i];
			// 清除所有的输入项
			if (item.tabIndex > 0) {
				if (item.type == "text" || item.type == "password" || item.type == "hidden")  item.value = "";
			}
		}

		var obj = ele.getElementsByTagName("TEXTAREA");
		for (i = 0; i < obj.length; i++) {
			item = obj[i];
			// 清除所有的输入项
			if (item.tabIndex > 0) {
				item.value = "";
			}
		}
}
 //检测字段值是否存在
function fncheckComm_org(table,col_key,col_arry){
    var ischeck="false";
    strXml="<xml>";
    strXml+="<TABLE>"+table+"</TABLE>";
    strXml+="<sels>*</sels>";
    for(var i=0;i<col_arry.length;i++){
     strXml+="<"+col_key[i]+">='"+col_arry[i]+"'</"+col_key[i]+">";
    }
    strXml+="</xml>";

    var url_str=server_url+"/servlet/mnbservlet?mod_name=com.mnbs.sysmanager.DoXmlH.getCheck";
    myAjax = new Ajax.Request(url_str,{asynchronous:false,contentType:'text/xml',encoding:'UTF-8',method:'post',postBody:strXml,onSuccess:
    function fnGet_asy(originalRequest){
      resp=originalRequest.responseText;
      if(errCheck(resp)){
          results = resp.evalJSON();
           ischeck=results.ret;
         }
     }
    });
  return ischeck;
}
 //检测字段值是否存在
function fncheckComm(table,col_arry){
    var ischeck="false";
    strXml="<xml>";
    strXml+="<TABLE>"+table+"</TABLE>";
    strXml+="<sels>*</sels>";
    for(var i=0;i<col_arry.length;i++){
     strXml+="<"+col_arry[i]+">='"+strim($(''+col_arry[i]+'_I').value)+"'</"+col_arry[i]+">";
    }
    strXml+="</xml>";
  //  alert(strXml);
    var url_str=server_url+"/servlet/mnbservlet?mod_name=com.mnbs.sysmanager.DoXmlH.getCheck";
    myAjax = new Ajax.Request(url_str,{asynchronous:false,contentType:'text/xml',encoding:'UTF-8',method:'post',postBody:strXml,onSuccess:
    function fnGet_asy(originalRequest){
      resp=originalRequest.responseText;
      if(errCheck(resp)){
          results = resp.evalJSON();
           ischeck=results.ret;
         }
     }
    });
  return ischeck;
}
//检测字段值是否存在
function fncheckComm_mod(table,col_arry,nowkey,nowprimary){
    var ischeck="false";
    strXml="<xml>";
    strXml+="<TABLE>"+table+"</TABLE>";
    strXml+="<sels>*</sels>";
    for(var i=0;i<col_arry.length;i++){
     strXml+="<"+col_arry[i]+">='"+strim($(''+col_arry[i]+'_D').value)+"'</"+col_arry[i]+">";
    }
   // strXml+="<"+nowkey+"><>'"+nowprimary+"'</"+nowkey+">";
  if(nowkey!='')
    strXml+="<"+nowkey+"> not in('"+nowprimary+"')</"+nowkey+">";
    strXml+="</xml>";
   // alert(strXml);
    var url_str=server_url+"/servlet/mnbservlet?mod_name=com.mnbs.sysmanager.DoXmlH.getCheck";
    myAjax = new Ajax.Request(url_str,{asynchronous:false,contentType:'text/xml',encoding:'UTF-8',method:'post',postBody:strXml,onSuccess:
    function fnGet_asy(originalRequest){
      resp=originalRequest.responseText;
      if(errCheck(resp)){
          results = resp.evalJSON();
           ischeck=results.ret;
         }
     }
    });
  return ischeck;
}

function getTimeSec(){
     d = new Date();
     s=d.getSeconds()+'';
     s+= d.getUTCMilliseconds()+'';
   return s;
}
//检查textarea内容长度
function fncheckTextArea(note,length){
  if(document.getElementById(note).value.length>length){
   return true;
  }
  return false;
}
//************************************************
function strim(str){
  return str.replace(/(^\s*)|(\s*$)/g, "");
}

function MM_findObj(n, d) { //v4.01
  var p,i,x;  if(!d) d=document; if((p=n.indexOf("?"))>0&&parent.frames.length) {
    d=parent.frames[n.substring(p+1)].document; n=n.substring(0,p);}
  if(!(x=d[n])&&d.all) x=d.all[n]; for (i=0;!x&&i<d.forms.length;i++) x=d.forms[i][n];
  for(i=0;!x&&d.layers&&i<d.layers.length;i++) x=MM_findObj(n,d.layers[i].document);
  if(!x && d.getElementById) x=d.getElementById(n); return x;
}

function MM_validateForm() { //v4.0
  var i,p,q,nm,test,num,min,max,errors='',args=MM_validateForm.arguments;
  for (i=0; i<(args.length-2); i+=3) { test=args[i+2]; val=MM_findObj(args[i]);
    if (val) { nm=val.name; if ((val=val.value)!="") {
      if (test.indexOf('isEmail')!=-1) { p=val.indexOf('@');
        if (p<1 || p==(val.length-1)) errors+=' '+nm+'：请填写正确的EMAIL地址。.\n';
      } else if (test!='R') {
        if (isNaN(val)) errors+='- '+nm+' must contain a number.\n';
        if (test.indexOf('inRange') != -1) { p=test.indexOf(':');
          min=test.substring(8,p); max=test.substring(p+1);
          if (val<min || max<val) errors+='- '+nm+' must contain a number between '+min+' and '+max+'.\n';
    } } } else if (test.charAt(0) == 'R') errors += '- '+nm+' is required.\n'; }
  } if (errors) alert(errors);
  document.MM_returnValue = (errors == '');
}


  
