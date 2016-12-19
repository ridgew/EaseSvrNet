var ProductItems = ["", "业务数据", "业务协议", "业务平台", "运营商", "业务状态", "计费模式"];

//window.onerror = function(e) {
//    if (top.console) {
//        top.console.log(e);
//    }
//    else {
//        alert(e);
//    }
//};

// Array Remove - By John Resig (MIT Licensed)
Array.prototype.remove = function(from, to) {
    var rest = this.slice((to || from) + 1 || this.length);
    this.length = from < 0 ? this.length + from : from;
    return this.push.apply(this, rest);
};

// 选择与反选
function SetCheck(chkBxName) {
    var objArray = document.getElementsByName(chkBxName);
    if (null != objArray) {
        for (var i = 0; i < objArray.length; i++) {
            objArray.item(i).checked = !(objArray.item(i).checked);
        }
    }
}

/*  FormItem[@type=radio|checkbox]标签的数据绑定 */
function HtmlItemsRestore(name, values) {
    var multiValChar = ",";
    var items = document.getElementsByName(name);
    var findNext = (values.indexOf(multiValChar) != -1);
    this.IsChecked = function(val) {
        if (findNext == true) {
            var strSource = multiValChar + values + multiValChar;
            var strChk = multiValChar + val + multiValChar;
            return (strSource.indexOf(strChk) != -1);
        }
        else {
            return (values == val);
        }
    };

    for (var i = items.length - 1; i >= 0; i--) {
        if (this.IsChecked(items[i].value)) {
            items[i].checked = true; if (!findNext) { break; }
        }
    }
}

/*  HtmlSelect标签的数据绑定 */
// update 2007-4-27 by Ridge Wong
function HtmlSelectRestore(name, values, selectBind) {
    var multiValChar = ",";
    var selObj = document.getElementsByName(name)[0];
    var findNext = (values.indexOf(multiValChar) != -1);
    this.IsChecked = function(val) {
        if (findNext == true) {
            var strSource = multiValChar + values + multiValChar;
            var strChk = multiValChar + val + multiValChar;
            return (strSource.indexOf(strChk) != -1);
        }
        else {
            return (values == val);
        }
    };

    for (var i = selObj.options.length - 1; i >= 0; i--) {
        if (this.IsChecked(selObj.options[i].value)) {
            selObj.options[i].selected = true;
            if (selectBind != null && typeof (selectBind) == "object") {
                selectBind.Object = selObj.options[i];
                selectBind.value = selObj.options[i].value;
                selectBind.text = selObj.options[i].text;
                selectBind.bind();
            }
            if (!findNext) { break; }
        }
    }
}

//从键值对数组返回自定义字段的对象数组
function getObjectFromDictionaryArray(dicArray, strFields) {
    var objArray = [];
    var currentObj = null;
    if (dicArray != null) {
        for (var i = 0; i < dicArray.length; i++) {
            currentObj = new Object();
            for (var k = 0; k < dicArray[i].length; k++) {
                if (strFields) {
                    if (("," + strFields + ",").indexOf("," + dicArray[i][k]["Key"] + ",") != -1) {
                        currentObj[dicArray[i][k]["Key"]] = dicArray[i][k]["Value"];
                    }
                }
                else {
                    currentObj[dicArray[i][k]["Key"]] = dicArray[i][k]["Value"];
                }
            }
            objArray[i] = currentObj;
        }
    }
    return objArray;
}

function setObjectDictionaryArrayItem(dictArray, key, val, append) {
    var hasSet = false;
    for (var i = 0; i < dictArray.length; i++) {
        if (dictArray[i]["Key"] == key) {
            if (append) {
                dictArray[i]["Value"] = dictArray[i]["Value"] + ',' + val;
            }
            else {
                dictArray[i]["Value"] = val;
            }
            hasSet = true;
            break;
        }
    }
    if (!hasSet) {
        var objCurrent = new Object();
         objCurrent["Key"] = key;
         objCurrent["Value"] = val;
         dictArray[dictArray.length] = objCurrent;
    }
}

//获取表单对象的键值词典表形式
function getObjectDictionaryArray(objFrm) {
    var oDictArray = new Array();
    var objCurrent, strType;
    //top.console.log(objFrm);
    $.each(objFrm, function(index, dom) {
        //alert(dom.innerHTML);
        if (dom.getAttribute("type")) {
            strType = dom.getAttribute("type").toLowerCase();
        }
        else {
            strType = dom.tagName;
        }
        if (dom.id || dom.name) {
            objCurrent = new Object();
            objCurrent["Key"] = dom.name || dom.id;
            objCurrent["Value"] = $(dom).val();
            oDictArray[oDictArray.length] = objCurrent;
        }
        else {
            switch (strType) {
                case 'radio':
                case 'checkbox':
                    if (dom.checked) {
                        setObjectDictionaryArrayItem(oDictArray, dom.name, dom.value, strType == 'checkbox')
                    }
                    break;
                default:
                    setObjectDictionaryArrayItem(oDictArray, dom.name, dom.value, false)
                    break;
            }
        }
    });
    return oDictArray;
}

function getValueByName(strName) {
    var objCol = document.getElementsByName(strName);
    var strType = (objCol[0].getAttribute("type")) ? objCol[0].getAttribute("type").toLowerCase() : objCol[0].tagName;
    var strReturnValue = "";
    switch (strType) {
        case "radio":
            for (var i = 0; i < objCol.length; i++) {
                if (objCol[i].checked) {
                    strReturnValue = objCol[i].value;
                    break;
                }
            }
            break;
        case "checkbox":
            for (var i = 0; i < objCol.length; i++) {
                if (objCol[i].checked) {
                    strReturnValue += "," + objCol[i].value;
                }
            }
            if (strReturnValue.length > 1) {
                strReturnValue = strReturnValue.substr(1);
            }
            break;
        default:
            strReturnValue = objCol[0].value;
            break;
    }
    return strReturnValue;
}

//绑定表单数据
function setFormBind(frmObj, objData) {
    var element,objCol, strType = '';
    for (var key in objData) {
        objCol = document.getElementsByName(key);
        if (objCol && objCol.length) {
            element = objCol[0];
            if (element.getAttribute("type")) {
                strType = element.getAttribute("type").toLowerCase();
            }
            else {
                strType = element.tagName;
            }
            switch (strType) {
                case 'radio':
                case 'checkbox':
                    if (objData[key] != null) {
                        HtmlItemsRestore(element.name, objData[key].toString());
                    }
                    break;
                case 'select':
                    if (objData[key] != null) {
                        HtmlSelectRestore(element.name, objData[key].toString(), objData[key + '-Bind']);
                    }
                    break;
                default:
                    element.value = objData[key];
                    break;
            }
            
        }
    }
}


//绑定指定ID的select
function OptionBind(optID, arrObject, txtName, valName, selValue) {
    var jbObj = $("#" + optID);
    jbObj.empty();
    for (var i = 0; i < arrObject.length; i++) {
        $("<option value='" + arrObject[i][valName] + "'>" + arrObject[i][txtName] + "</option>").appendTo(jbObj);
        if (selValue && selValue != "") {
            if (arrObject[i][valName].toString() == selValue.toString()) {
                //alert(jbObj.children().get(i).outerHTML);
                jbObj.children().get(i).setAttribute("selected", "selected");
                //jbObj.children().get(i).selected = true;
            }
        }
    }
}

//复制数组数据到目标数组
function CopyTo(arrObj, tarArrObj) {
    for (var i = 0; i < arrObj.length; i++) {
        tarArrObj[tarArrObj.length] = arrObj[i];
    }
    return tarArrObj;
}

//等待
//在breakFlgfn返回为true时执行doFn
function Wait(breakFlgfn, doFn) {
    if (breakFlgfn()) {
        doFn();
    }
    else {
        window.setTimeout(function() {
            Wait(breakFlgfn, doFn);
        }, 50);
    }
}

//遮罩整个窗口直至函数unblockFnFlg返回为true
function BlockUI(msg, unblockFnFlg) {
    $.blockUI({ message: msg });
    //取消遮盖的定时器
    window.unBlock = window.setInterval(function() {
        if (unblockFnFlg()) {
            window.clearInterval(window.unBlock);
            $.unblockUI();
        }
    }, 50);
}