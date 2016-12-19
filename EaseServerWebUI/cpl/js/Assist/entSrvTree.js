/// <reference path="../jquery.js" />
$(document).ready(function() {

    var strTitle = '接入服务器业务配置';
    var jQueryUL = $('#tree');
    var strModeCode = '1.3.99.1.4';
    var strPostData = '{}';
    var jsonConfig = { 'ID': 'SERVICE_ID', 'Name': 'SERVICE_NAME', 'hasChild': 'no' };
    var strRightCode = '3.99.1.4';

    var MenuItem = function() {
        var obj = this;
        return $('<div class="title"><img border="0" align="absmiddle" src="../images/type.gif" />' + '[' + obj[jsonConfig.ID] + ']' + obj[jsonConfig.Name] + '</div>').click(function() {
            top.deskTabs.getFrameRight().contentWindow.location.href = 'EntSrvEdit.html?t=edit&sid=' + obj[jsonConfig.ID];
        });
    };

    getTree(strTitle, jQueryUL, strModeCode, strPostData, jsonConfig, strRightCode, null, null, MenuItem);
});                 