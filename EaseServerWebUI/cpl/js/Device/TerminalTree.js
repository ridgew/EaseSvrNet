jQuery(function($) {
    $("#root_li").attr({ "style": "cursor:pointer;" })
    window.top.init_contentMenu($("#root_ul"), '3.9.3.1');
    var strTitle = '虚拟设备管理'; //标题
    var jQueryUL = $('#tree'); //树根节点，一般都不变
    var strModeCode = '1.3.9.3.4'; //功能序号
    var strPostData = '{}'; //需要向后台传递的参数
    var jsonConfig = { 'ID': 'MT_ID', 'Name': 'MT_Name', 'hasChild': 'MT_Name' }; //返回数据的ID，数据名，是否有子级树
    var strRightCode = '3.9.3.2'; //右键菜单编号
    //B_ID, B_Name, B_OrderNum, B_Info, B_AddTime
    var imgObj = function() {
        var _this = this;
        return $('<div class="title"><img border="0" align="absmiddle" src="../images/type.gif" />[' + this[jsonConfig.ID] + ']' + this[jsonConfig.Name] + '</div>').click(function() {
            top.deskTabs.getFrameRight().contentWindow.location.href = 'MobileList.html?MT_ID=' + _this[jsonConfig.ID];
        });
    };

    var getChild = function() {
        var strTitle = '机型'; //标题
        var jQueryUL = $(this).find('ul'); //树根节点，一般都不变
        var strModeCode = '1.3.9.2.11'; //功能序号
        var strPostData = jQuery.JSON.encode({ MT_ID: this.value }); //需要向后台传递的参数
        var jsonConfig = { 'ID': 'M_ID', 'Name': 'M_Name', 'hasChild': 'no' }; //返回数据的ID，数据名，是否有子级树
        var strRightCode = '3.9.3.3'; //右键菜单编号
        var imgObj = function() {
            var _this = this;
            return $('<div class="title"><img border="0" align="absmiddle" src="../images/type.gif" /> ' + this[jsonConfig.Name] + '</div>').click(function() {
                top.deskTabs.getFrameRight().contentWindow.location.href = 'MobileEdit.html?M_ID=' + _this[jsonConfig.ID];
            });
        };
        getTree(strTitle, jQueryUL, strModeCode, strPostData, jsonConfig, strRightCode, null, null, imgObj);
    };
    getTree(strTitle, jQueryUL, strModeCode, strPostData, jsonConfig, strRightCode, getChild, null, imgObj);
});