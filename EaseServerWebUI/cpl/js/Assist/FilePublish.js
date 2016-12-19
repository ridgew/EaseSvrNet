$(function() {
    var ajaxConfig = { url: './service/1.3.99.2.0' };
    var objData;
    var ajaxObj = new top.AJAXFunction(ajaxConfig);
    ajaxObj.SuFun = function(responseObj) {
        objData = responseObj.d.Data;
        loadData();
    }
    ajaxObj.SendFun();

    function loadData() {
        if (objData == null || objData.length == 0) {
            $("#ExcuteResult").append("<h1>文件发布完成</h1>");
        }
        else {
            $("#ExcuteResult").append("<h1>文件发布结束</h1>");
            for (i = 0; i < objData.length; i++) {
                $("#ExcuteResult").append("<div>" + objData[i] + "</div>")
            }
        }
    }
});