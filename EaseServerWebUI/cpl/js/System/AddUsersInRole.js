/// <reference path="../jquery.js" />
$(document).ready(function() {
    var roleName = $.QueryString().roleName;
    $('#titRole').html($.QueryString().tit);
    var grid = new GridPanel({
        store: new Store({ url: '../service/1.3.5.3.2' }),
        renderTo: 'userList',
        Columns: [
    { header: '用户编号', dataIndex: 'UserId', sortable: false, allowHide: false, isShow: true, className: 'th01' },
	{ header: '用户名', dataIndex: 'UserName', sortable: false, allowHide: false, isShow: true, className: 'th01' },
	{ header: '真实姓名', dataIndex: 'RealName', sortable: false, allowHide: true, isShow: true, className: 'th01' },
	{ header: '电子邮箱', dataIndex: 'Email', sortable: false, allowHide: true, isShow: true, className: 'th01' },
	{ header: '审核', dataIndex: 'IsApproved', sortable: false, allowHide: true, isShow: true, className: 'th01', render: function(value, record) { if (value) { return '<img src="../images/ch_yes.gif"/>' } else { return '<img src="../images/ch_no.gif"/>' } } },
	{ header: '锁定', dataIndex: 'IsLockedOut', sortable: false, allowHide: true, isShow: true, className: 'th01', render: function(value, record) { if (value) { return '<img src="../images/ch_no.gif"/>' } else { return '<img src="../images/ch_yes.gif"/>' } } },
	{ header: '注册时间', dataIndex: 'CreateDate', sortable: false, allowHide: true, isShow: true, className: 'th01', render: function(value, record) { return record.CreateDate.Value; } },
	{ header: '最近登录时间', dataIndex: 'LastLoginDate', sortable: false, allowHide: true, isShow: true, className: 'th01', render: function(value, record) { return record.LastLoginDate.Value; } }
	]
    }
    );
    $('#aAdd').click(function() {
        var userIds = grid.GetSelection('UserId');
        if (userIds.length > 0) {
            var addAjax = new top.AJAXFunction({ url: './service/1.3.5.2.6', param: { userIds: userIds, roleName: roleName} });
            addAjax.SuFun = function(json) {
                if (json.d.Status == 10034) {
                    top.deskTabs.getFrameRight().contentWindow.__grid.RefreshPage();
                }
            };
            addAjax.SendFun();
        }
        else
            alert('请选择用户');
    });
    grid.doRender();
});