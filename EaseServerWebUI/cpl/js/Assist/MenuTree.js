/// <reference path="../jquery.js" />
$(document).ready(function() {
	var strTitle = '菜单帮助';
	var jQueryUL = $('#tree');
	var strModeCode = '1.3.5.1.3';
	var strPostData = '{parentId:0}';
	var jsonConfig = { 'ID': 'MenuId', 'Name': 'MenuName', 'hasChild': 'HasChildren' };
	var strRightCode = '';

	var MenuItem = function() {
		var obj = this;
		return $('<div class="title"><img border="0" align="absmiddle" src="../images/type.gif" />' + obj.MenuName + '</div>').click(function() {
			top.deskTabs.getFrameRight().contentWindow.location.href = 'HelpEdit.html?MenuId=' + obj.MenuId;
		});
	};

	var fnGetTree = function() {
		var strTitle = '菜单帮助';
		var strModeCode = '1.3.5.1.3';
		var strPostData = '{parentId:' + this.value + '}';
		var jsonConfig = { 'ID': 'MenuId', 'Name': 'MenuName', 'hasChild': 'HasChildren' };
		getTree(strTitle, $(this).find('>ul'), strModeCode, strPostData, jsonConfig, strRightCode, fnGetTree, null, MenuItem);
	};

	getTree(strTitle, jQueryUL, strModeCode, strPostData, jsonConfig, strRightCode, fnGetTree, null, MenuItem);
});