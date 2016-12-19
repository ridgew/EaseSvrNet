function BindSingleDelete(e) {
    var tContainer = $(this).closest("div");
    tContainer.parent()[0].removeChild(tContainer[0]);
}

function CreatAndBind(item) {
    var tpt = $('div.ftptSet');
    $('<div class="ftptSet nTpt" style="width:80%">' + tpt[0].innerHTML.replace(/(tpt\w+)/ig, '$1' + tpt.length).replace('参数0', '参数' + tpt.length)
        + '</div>').appendTo(tpt.parent());
    //top.console.log(tpt.length);
    var aBtnArray = $($('div.ftptSet')[tpt.length]).find('a');
    if (aBtnArray.length != 1) {
        alert('错误：客户端控件构建失败！');
        return;
    }
    else {
        aBtnArray[0].onclick = BindSingleDelete;
    }
    
    if (item) {
        //top.console.log(item);
        $(tpt[tpt.length]).find('input[name="tptParamName' + tpt.length + '"]').val(item['Name'] || '');
        $(tpt[tpt.length]).find('input[name="tptParamVal' + tpt.length + '"]').val(item['Val'] || '');
        $(tpt[tpt.length]).find('input[name="tptParamMemo' + tpt.length + '"]').val(item['Memo']);
        $(tpt[tpt.length]).find('select[name="tptParamType' + tpt.length + '"]').val(item['Type']);
    }
    else {
        $(tpt[tpt.length]).find('input[name="tptParamName' + tpt.length + '"]').val('');
        $(tpt[tpt.length]).find('input[name="tptParamVal' + tpt.length + '"]').val('');
        $(tpt[tpt.length]).find('input[name="tptParamMemo' + tpt.length + '"]').val(''); 
        $(tpt[tpt.length]).find('input[name="tptParamType' + tpt.length + '"]').val('0');
    }
}

var queryStr = window.location.search.replace("?", "");
jQuery(function($) {

    $('#btn_Add').click(function() { CreatAndBind(); });

    //获取数据
    window.protocol = "1.3.0.5.0";
    if (queryStr != null && parseInt(queryStr) > 0) {
        var id = parseInt(queryStr);
        $.ajax({
            type: "POST", url: "../service/" + window.protocol + ".4",
            contentType: "application/json; charset=utf-8", dataType: "json",
            data: JSON.stringify({ identity: id, protocol: window.protocol + ".4" }),
            error: function(xmlObj, Status) {
                alert('操作失败！');
            },
            success: function(json) {
                var data = json.d.Data;

                //tDayStart|2|20090401|开始日期$tDayEnd|2|20090531|结束日期
                //top.console.log(data.ParanameList);
                if (data.ParanameList != '') {
                    var itemArr = data.ParanameList.split('$');
                    var iSetArr;
                    for (var i = 0, j = itemArr.length; i < j; i++) {
                        iSetArr = itemArr[i].split('|');
                        CreatAndBind({ Name: iSetArr[0], Val: iSetArr[2], Type: iSetArr[1].toString(), Memo: iSetArr[3] })
                    }
                }

                $.each($('form')[0], function(index, dom) {
                    try {
                        //top.console.log(dom);
                        $(dom).val(data[dom.id].toString(10));
                    } catch (e) { }
                });
            }
        });
    }

    //重置按钮绑定
    $('#btn_Reset').click(function() {
        $('form').each(function(idx, dom) {
            dom.reset();
        });
    });

    //保存
    $('#btn_Save').click(function() {

        var objForm = $('form')[0];
        if (!Validator.Validate(objForm, 2)) return;
        var protocolAct = window.protocol + (($(objForm[0]).val() == "0") ? ".1" : ".2");
        var exData = getObjectDictionaryArray(objForm);
        var jTpt = $('div.ftptSet');

        if (exData.length == 14 && jTpt.length > 1) {
            var nContainer, nItems;
            for (var i = 1; i < jTpt.length; i++) {
                nContainer = jTpt[i];
                nItems = $(nContainer).find('input');
                for (var j = 0; j < nItems.length; j++) {
                    exData[exData.length] = { Key: nItems[j].name, Value: nItems[j].value };
                }
                nItems = $(nContainer).find('select');
                for (var j = 0; j < nItems.length; j++) {
                    exData[exData.length] = { Key: nItems[j].name, Value: nItems[j].value };
                }
            }
        }
        
        $.ajax({
            type: "POST", url: "../service/" + protocolAct,
            contentType: "application/json; charset=utf-8",
            dataType: "json", data: JSON.stringify({ protocol: protocolAct, entryData: exData }),
            beforeSend: function(xhr) {
                $.blockUI({ message: "正在处理..." });
            },
            error: function(xmlObj, Status) { $.unblockUI(); alert('操作失败！'); },
            success: function(data) {
                if (data.d.Status == 0) {
                    alert(data.d.Message);
                }
                else {
                    //$(objForm[0]).val()
                    alert("操作成功");
                }
                $.unblockUI();
            }
        });

    });

});