if (typeof (console) == undefined) console = { log: function(x) { alert(x) } };
function stopEvent(evt) {
	var e = (evt) ? evt : window.event;
	if (window.event) {e.cancelBubble = true; } else { e.stopPropagation(); }
}
if (top != self) top.location.href = self.location.href;
jQuery(function() {
	WIMP.Desktop.initialize();
	WIMP.DesktopParts.Separator.toggleCallBack = function(display) {
		$("#line-shadow").css("display", display);
	};
	WIMP.Desktop.resize();

	var sp = new Separator("line", true, "left", "right");
	sp.onCreatedShadow = function(objShadow) {
		var arrow = objShadow.getElementsByTagName("div")[0];
		if (arrow) {
			var oldId = arrow.id;
			arrow.id += "_shadow";
			arrow.onmouseover = function() {
				$(this).addClass("lineicolefthight");
			};
			arrow.onmouseout = function() {
				$(this).removeClass("lineicolefthight");
			};
			objShadow.style.zIndex = 200001;
//			objShadow.style.opacity = '0.9';
//			objShadow.style.filter = 'alpha(opacity=90)';
			objShadow.style.height = WIMP.DesktopParts.LeftPanel.getTotalHeight() + "px";
			arrow.onclick = function(evt) {
				var tCb = function(display) {
					$(objShadow).css("display", display);
				};
				WIMP.DesktopParts.Separator.toggle(tCb);
				stopEvent(evt);
			};
		}
	};

	sp.canDrag = function(e, dragObj, x, y) {
		return ($("#left").css("display") != "none");
	};

	//确认可放/更新关联元素大小
	sp.canDrop = function(e, dragObj, x, y) {
		return (dragObj && dragObj.id == 'line_shadow');
	};

	//修补边框位移
	sp.onDropCallBack = function(objDragTarget) {
		if (objDragTarget && objDragTarget.id == 'line_shadow') {
			WIMP.DesktopParts.LeftPanel.setTotalWidth(parseInt(objDragTarget.style.left));
			objDragTarget.style.left = (parseInt(objDragTarget.style.left) + 2) + "px";
			$("#right").css("margin-left", (9 + parseInt($("#left").css("width"))) + "px");
			WIMP.Desktop.resize();
		}
	};

	sp.initialize();
	//WIMP.Desktop.collapseLeft(true);
	WIMP.DesktopParts.LeftPanel.MenuParts.showMenuFrame(false);
	window.onresize = function() {
		WIMP.Desktop.resize();
		if ($("#left").css("display") != "none") sp.resetShadow();
	};
	window.status = 'EASE业务平台';
	
	//个人配置
	var userId;
	var delAjax = new top.AJAXFunction({ url: './service/1.3.5.3.9', param: {},async:false });
    delAjax.SuFun = function(json) {
        userId=json.d.UserId;
    };
    delAjax.SendFun();
    
    $("#GRPZ").click(
    function(){
        deskTabs.iframeOpen('个人配置','./System/EditOneself.html?userId='+userId);
    });
    
    //状态栏
    
    var roleAjax = new top.AJAXFunction({ url: './service/1.3.5.2.1' });
    roleAjax.SuFun = function(json) {
        var roleList = json.d.Data;
 
        var rolename="角色：";
        var getAjax = new top.AJAXFunction({ url: './service/1.3.5.3.4', param: { userId: userId} });
        getAjax.SuFun = function(json) {
            json = json.d.Data;
            $(".th02 span").text("用户名："+json.UserName);
            $(".th03 span").text("姓名："+json.RealName);
            $(json.Roles).each(function(i) {
                var role=this;
                $(roleList).each(function(i) {
                    if(this.RoleName==role)
                    {
                        rolename+=this.Description+",";
                    }
                });
            });
            
            rolename=rolename.substring(0,rolename.length-1);
            
            $(".th04 span").text(rolename);
        };
        getAjax.SendFun();
    
    };
    roleAjax.SendFun();
    
    var date=new Date();
    $(".th06 span").text("日期："+date.toLocaleDateString());
    $(".th01 span").text("服务器："+document.domain);
});

var ___CurrentTreeNodeParent = null;
