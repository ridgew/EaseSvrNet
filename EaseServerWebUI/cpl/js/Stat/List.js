/*处理业务块的列表操作*/
function openTab(title, url, useleftFrame) {
    if (useleftFrame) {
        top.deskTabs.iframeOpen(title, url, top.deskTabs.getFrameLeft().id)
    }
    else {
        top.deskTabs.iframeOpen(title, url)
    }
}

var qStrArr = request.QueryStrings();
var grid = new GridPanel({
    store: new Store({ url: '../service/1.3.0.5.0.6',
        params: { protocol: '1.3.0.5.0.6', selectFields: 'Id,Title,IsProcedure,CreateTime', orderbyFields: null, isDesc: true },
        reader: { dataRoot: 'Data[0].rows', pages: 'PageCount', records: 'RecordCount', jsonRoot: 'd', pageSize: 'PageSize' }
    }),
    renderTo: 'row3',
    //cellSelection: false,
    Columns: [
        { header: '编号', dataIndex: 'Id', sortable: false, allowHide: false, isShow: true, className: 'th01' },
	    { header: '项目名称', dataIndex: 'Title', sortable: false, allowHide: false, isShow: true, className: 'th01' },
	    { header: '存储过程', dataIndex: 'IsProcedure', sortable: false, allowHide: false, isShow: true, className: 'th01',
	        render: function(value, record) {
	            return (value) ? '<img src="../images/ch_yes.gif"/>' : '<img src="../images/ch_no.gif"/>';
	        }
	    },
	    { header: '创建时间', dataIndex: 'CreateTime', sortable: false, allowHide: false, isShow: true, className: 'th01',
	        render: function(value, record) {
	            return value.Value;
	        }
	    },
	    { header: '操作', dataIndex: 'Id', isShow: true, className: 'th01',
	        render: '<span class="sp04"><a href="javascript:void(0);" onclick="javascript:openTab(\'统计规则设置\',\'./Stat/edit.html?{Id}\', false);">[修改]</a></span>'
	    }
	]
});

//top.console.log(grid.store.GetPageParams());
//grid.store.params["ClassID"] = "2";
//grid.store.GetPageParams();

jQuery(function($) {
    grid.doRender();

    //事件绑定
    $('#btnDelete').click(function(e) {
        e.preventDefault();
        var ids = grid.GetSelection('Id');
        if (ids.length > 0) {
            if (confirm("确定要删除你选中的规则编号？")) {
                var ajaxDelete = new top.AJAXFunction({ url: './service/1.3.0.5.0.7',
                    param: { protocol: '1.3.0.5.0.7', demo: [{ Key: '{Constrains}', Value: 'In("Id" , "' + ids.join(',') + '")'}] }
                });
                ajaxDelete.SuFun = function(data, status) {
                    alert(data.d.Message);
                    grid.RefreshPage();
                };
                ajaxDelete.SendFun();
            }
        } else {
            alert("请选择你要删除的规则编号！");
        }
    }).css('cursor', 'pointer');

    $('#btnRefreshPage1').click(function(e) {
        e.preventDefault();
        grid.RefreshPage();
    });

    $('#aAdd').click(function(e) { openTab('统计规则设置', './Stat/edit.html', false); });

});