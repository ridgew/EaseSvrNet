var _t = jQuery.QueryString().t;
var _sid = jQuery.QueryString().sid;
$(function() {
    var sendCode;
    if (_t == 'edit') {
        var ajaxConfig = { url: './service/1.3.99.1.3', data: '{"serviceid":"' + _sid + '"}' };
        var objData;
        var ajaxObj = new top.AJAXFunction(ajaxConfig);
        ajaxObj.SuFun = function(responseObj) {
            objData = responseObj.d.Data;
            loadData();
			$("#ich").unblock();
        }
		$("#ich").block({ message: '数据加载中...', css: { width: "30%", left: "70%", top: "10%"} });
        ajaxObj.SendFun();

        function loadData() {
            $('#oldsid').val(_sid);
            $('#SERVICE_ID').val(objData.SERVICE_ID);
            $('#SERVICE_NAME').val(objData.SERVICE_NAME);
            $('#SERVICE_VERSION').val(objData.SERVICE_VERSION);
            $('#SERVICE_ENCODE').val(objData.SERVICE_ENCODE);
            $('#TXT_IN_ENCODE').val(objData.TXT_IN_ENCODE);
            $('#TXT_OUT_ENCODE').val(objData.TXT_OUT_ENCODE);
            $('#FIRST_MODE').val(objData.FIRST_MODE);
            $('#FIRST_ACTION_TYPE').val(objData.FIRST_ACTION_TYPE);
            $('#FIRST_ACTION').val(objData.FIRST_ACTION);
            $('#CLIENT_ROOT_URI').val(objData.CLIENT_ROOT_URI);
            $('#SERVICE_URL').val(objData.SERVICE_URL);
            $('#CONNECT_TYPE').val(objData.CONNECT_TYPE);
            $('#LINK_URL_PREFIX').val(objData.LINK_URL_PREFIX);
            $('#RES_URL_PREFIX').val(objData.RES_URL_PREFIX);
            $('#SERVICE_INDEX_URL').val(objData.SERVICE_INDEX_URL);
            $('#SERVICE_REG_URL').val(objData.SERVICE_REG_URL);
            $('#SERVICE_HELP_URL').val(objData.SERVICE_HELP_URL);
            $('#SERVICE_DATABASE').val(objData.SERVICE_DATABASE);
            $('#SERVICE_UserAssignFormat').val(objData.SERVICE_UserAssignFormat);
            $('#MemoryCacheTime').val(objData.MemoryCacheTime);
            $('#IsolatedCacheTime').val(objData.IsolatedCacheTime);
            $('#CacheMode').val(objData.CacheMode);
            $('#SplitRate').val(objData.SplitRate);
            $('#PageParamProcess').val(objData.PageParamProcess);
        }

        $('#btn_Reset').click(function() {
            loadData();
        });

        sendCode = './service/1.3.99.1.1';
    } else {
        $('#btn_Reset').click(function() {
            $('#theForm').reset();
        });
        $('#SERVICE_ID').parent().parent().hide();
        sendCode = './service/1.3.99.1.0';
    }

    $('#btn_Save').click(function() {
        vSERVICE_ID = $('#SERVICE_ID').val();
        vSERVICE_NAME = $('#SERVICE_NAME').val();
        vSERVICE_VERSION = $('#SERVICE_VERSION').val();
        vSERVICE_ENCODE = $('#SERVICE_ENCODE').val();
        vTXT_IN_ENCODE = $('#TXT_IN_ENCODE').val();
        vTXT_OUT_ENCODE = $('#TXT_OUT_ENCODE').val();
        vFIRST_MODE = $('#FIRST_MODE').val();
        vFIRST_ACTION_TYPE = $('#FIRST_ACTION_TYPE').val();
        vFIRST_ACTION = $('#FIRST_ACTION').val();
        vCLIENT_ROOT_URI = $('#CLIENT_ROOT_URI').val();
        vSERVICE_URL = $('#SERVICE_URL').val();
        vCONNECT_TYPE = $('#CONNECT_TYPE').val();
        vLINK_URL_PREFIX = $('#LINK_URL_PREFIX').val();
        vRES_URL_PREFIX = $('#RES_URL_PREFIX').val();
        vSERVICE_INDEX_URL = $('#SERVICE_INDEX_URL').val();
        vSERVICE_REG_URL = $('#SERVICE_REG_URL').val();
        vSERVICE_HELP_URL = $('#SERVICE_HELP_URL').val();
        vSERVICE_DATABASE = $('#SERVICE_DATABASE').val();
        vSERVICE_UserAssignFormat = $('#SERVICE_UserAssignFormat').val();
        var ajaxConfig = { url: sendCode, param: { SERVICE_ID: vSERVICE_ID, SERVICE_NAME: vSERVICE_NAME, SERVICE_VERSION: vSERVICE_VERSION, SERVICE_ENCODE: vSERVICE_ENCODE,
            TXT_IN_ENCODE: vTXT_IN_ENCODE, TXT_OUT_ENCODE: vTXT_OUT_ENCODE, FIRST_MODE: vFIRST_MODE, FIRST_ACTION_TYPE: vFIRST_ACTION_TYPE, FIRST_ACTION: vFIRST_ACTION,
            CLIENT_ROOT_URI: vCLIENT_ROOT_URI, SERVICE_URL: vSERVICE_URL, CONNECT_TYPE: vCONNECT_TYPE, LINK_URL_PREFIX: vLINK_URL_PREFIX, RES_URL_PREFIX: vRES_URL_PREFIX,
            SERVICE_INDEX_URL: vSERVICE_INDEX_URL, SERVICE_REG_URL: vSERVICE_REG_URL, SERVICE_HELP_URL: vSERVICE_HELP_URL,
            SERVICE_DATABASE: vSERVICE_DATABASE, oldid: _sid, SERVICE_UserAssignFormat: vSERVICE_UserAssignFormat,
            MemoryCacheTime: $('#MemoryCacheTime').val(),
            IsolatedCacheTime: $('#IsolatedCacheTime').val(),
            cacheMode: $('#CacheMode').val(),
			pageParamProcess: $('#PageParamProcess').val(),
			splitRate:$('#SplitRate').val()
        }
        };

        var ajaxObj = new top.AJAXFunction(ajaxConfig);
        ajaxObj.SuFun = function(responseObj) {
            alert('操作成功！');
            top.deskTabs.getFrameLeft().contentWindow.location.reload();
        }
        ajaxObj.SendFun();
    });
});