/// <reference path="../jquery.js" />
$(document).ready(function() {
    var strTitle = '角色管理';
    var jQueryUL = $('#tree');
    var strModeCode = '1.3.5.2.1';
    var strPostData = '{}';
    var jsonConfig = { 'ID': 'RoleId', 'Name': 'RoleName', 'hasChild': 'HasChildren' };
    var strRightCode = '3.5.2';

    var RoleItem = function() {
        var obj = this;
        var img = $('<img border="0" src="../images/role.gif" align="absmiddle"/>');
        var item = $('<span> ' + this.Description + '[' + this.RoleName + ']' + '</span>');
        item.click(function() { TreeClickFun(obj.RoleId, obj.RoleName, obj.Description); });
        return $('<div class="title"></div>').append(img).append(item).attr({ roleName: obj.RoleName, roleDescription: obj.Description });
    };

    getTree(strTitle, jQueryUL, strModeCode, strPostData, jsonConfig, strRightCode, null, null, RoleItem);
});

function TreeClickFun(roleId, roleName, roleDescription) {
    top.deskTabs.getFrameRight().contentWindow.location.href = 'EditRole.html?RoleId=' + roleId + '&RoleName=' + roleName + '&RoleDescription=' + roleDescription;
}