/// <reference path="../jquery.js" />
$(document).ready(function() {
    var strTitle = '菜单管理';
    var jQueryUL = $('#tree');
    var strModeCode = '1.3.5.1.3';
    var strPostData = '{parentId:0}';
    var jsonConfig = { 'ID': 'MenuId', 'Name': 'MenuName', 'hasChild': 'HasChildren' };
    var strRightCode = '3.5.1';

    var MenuItem = function() {
        var obj = this;
        var img = $('<img mid="mid0" enabled="1" border="0" align="absmiddle"/>');
        if (this.Enabled)
            MenuVisible(img.attr('enabled', '1'), '1');
        else
            MenuVisible(img.attr('enabled', '0'), '0');
        img.attr('mid', obj.MenuId).click(function() {
            var enabled = $(this).attr('enabled') == '1' ? '0' : '1';
            var val = (enabled == '0' ? false : true);
            var objImg = this;
            var ajax = new top.AJAXFunction({ url: './service/1.3.5.1.11', param: { menuIds: [$(this).attr('mid')], menuEnableds: [val]} });
            ajax.SuFun = function(json) {
                if (json.d.Status == 10012) {
                    $(objImg).attr('enabled', enabled);
                    MenuVisible(objImg, enabled);
                }
            };
            ajax.SendFun();
        });
        var item = $('<span> ' + obj.MenuName + '</span>');
        item.click(function() { TreeClickFun(obj.MenuId,$(this).parent().parent().parent().parent().attr("value"));})
       
        return $('<div class="title"></div>').append(img).append(item);
    };

    var fnGetTree = function() {
        var strTitle = '菜单管理';
        var strModeCode = '1.3.5.1.3';
        var strPostData = '{parentId:' + this.value + '}';
        var jsonConfig = { 'ID': 'MenuId', 'Name': 'MenuName', 'hasChild': 'HasChildren' };
        getTree(strTitle, $(this).find('>ul'), strModeCode, strPostData, jsonConfig, strRightCode, fnGetTree, null, MenuItem);
    };

    getTree(strTitle, jQueryUL, strModeCode, strPostData, jsonConfig, strRightCode, fnGetTree, null, MenuItem);
});

function TreeClickFun(menuId) {
    var FMenuId=arguments[1];
    top.deskTabs.getFrameRight().contentWindow.location.href = 'EditMenu.html?MenuId=' + menuId+'&FMenuId='+FMenuId;
}

function MenuVisible(img, value) {
    if (value == "1")
        $(img).attr('src', '../images/ch_yes.gif').attr('title', '点击设置为隐藏菜单');
    else if (value == "0")
        $(img).attr('src', '../images/ch_no.gif').attr('title', '点击设置为显示菜单');
}