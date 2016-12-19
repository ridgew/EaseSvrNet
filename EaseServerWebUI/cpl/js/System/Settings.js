/// <reference path="../jquery.js" />
var __grid;
$(document).ready(function() {
    $('#aAdd').click(function() {
        top.fn_add_div('./System/CreateParameter.html', null, ($(document).height() - 245) / 2, 780, 245, '增加系统参数');
    });

    __grid = new GridPanel({
        store: new Store({ url: '../service/1.3.5.5.1' }),
        pageNavigation: false,
        rowSelection: false,
        renderTo: 'paraList',
        Columns: [
    { header: '参数名称', dataIndex: 'Name', sortable: false, allowHide: false, isShow: true, className: 'th01' },
	{ header: '参数值', dataIndex: 'Value', sortable: false, allowHide: false, isShow: true, className: 'th01' },
	{ header: '功能描述', dataIndex: 'Description', sortable: false, allowHide: true, isShow: true, className: 'th01' },
	{ header: '操作', dataIndex: 'Name', allowHide: false, isShow: true, className: 'th01', render: '<span class="sp04"><a href="javascript:;" onclick="editParaByName(\'{Name}\')">[修改]</a><a href="javascript:;" onclick="deleteParaByName(\'{Name}\');">[删除]</a></span>' }
	]
    }
    );
    __grid.doRender();
});

function deleteParaByName(name) {
    if (confirm('警告：您确定要删除系统参数【' + name + '】吗？\n\n该操作可能会影响系统正常运行，请慎重选择！\n\n')) {
        var delAjax = new top.AJAXFunction({ url: './service/1.3.5.5.4', param: { name: name} });
        delAjax.SuFun = function(json) {
            if (json.d.Status == 10015)
                __grid.RefreshPage();
        };
        top.showMask();
        delAjax.SendFun();
    }
}

function editParaByName(name) {
    top.fn_add_div('./System/CreateParameter.html?name=' + name, null, ($(document).height() - 245) / 2, 780, 245, '修改系统参数');
}