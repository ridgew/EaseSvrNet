/*
* ContextMenu - jQuery plugin for right-click context menus
*
* Author: Chris Domigan
* Contributors: Dan G. Switzer, II
* Parts of this plugin are inspired by Joern Zaefferer's Tooltip plugin
*
* Dual licensed under the MIT and GPL licenses:
*   http://www.opensource.org/licenses/mit-license.php
*   http://www.gnu.org/licenses/gpl.html
*
* Version: r2
* Date: 16 July 2007
*
* For documentation visit http://www.trendskitchens.co.nz/jquery/contextmenu/
*
*/

(function($) {

    var menu, shadow, trigger, content, hash, currentTarget, curTop, curTopx;
    var defaults = {
        menuStyle: { listStyle: 'none', padding: '1px', margin: '0px', backgroundColor: '#fff', border: '1px solid #6298C7', width: '100px', fontSize: '12px' },
        itemStyle: { margin: '0px', color: '#000', display: 'block', cursor: 'default', padding: '3px', border: '1px solid #fff', backgroundColor: 'transparent' },
        itemHoverStyle: { border: '1px solid #0a246a', backgroundColor: '#2682C6', cursor: 'pointer', color: '#ffffff' },
        eventPosX: 'pageX', eventPosY: 'pageY',
        shadow: true, onContextMenu: null, onShowMenu: null
    };

    $.fn.contextMenu = function(id, options) {
        if (!menu) {                                      // Create singleton menu
            menu = $('<div id="jqContextMenu"></div>')
               .hide()
               .css({ position: 'absolute', zIndex: '9200001' })
               .appendTo('body')
               .bind('click', function(e) { e.stopPropagation(); });
        }
        if (!shadow) {
            shadow = $('<div></div>')
                 .css({ backgroundColor: '#000', position: 'absolute', opacity: 0.2, zIndex: '9200000' })
                 .appendTo('body')
                 .hide();
        }
        hash = hash || [];
        hash.push({
            id: id,
            menuStyle: $.extend({}, defaults.menuStyle, options.menuStyle || {}),
            itemStyle: $.extend({}, defaults.itemStyle, options.itemStyle || {}),
            itemHoverStyle: $.extend({}, defaults.itemHoverStyle, options.itemHoverStyle || {}),
            bindings: options.bindings || {},
            shadow: options.shadow || options.shadow === false ? options.shadow : defaults.shadow,
            onContextMenu: options.onContextMenu || defaults.onContextMenu,
            onShowMenu: options.onShowMenu || defaults.onShowMenu,
            eventPosX: options.eventPosX || defaults.eventPosX,
            eventPosY: options.eventPosY || defaults.eventPosY
        });

        var index = hash.length - 1;
        $(this).bind('contextmenu', function(e) {
            //改变背景和字体颜色
            function getRootNode(obj) {
                if (obj.parentNode && obj.parentNode.id != 'tree')
                    return getRootNode(obj.parentNode);
                else
                    return obj.parentNode;
            }
            $(getRootNode(this)).find('div.title').css({ 'backgroundColor': '', 'color': '#000000' });
            if (this.className.indexOf('title') > -1)
                $(this).css({ 'backgroundColor': '#2682C6', 'color': '#ffffff' });
            else
                $(this).find('>div.title').css({ 'backgroundColor': '#2682C6', 'color': '#ffffff' });

            // Check if onContextMenu() defined
            var bShowContext = (!!hash[index].onContextMenu) ? hash[index].onContextMenu(e) : true;
            if (bShowContext) display(index, this, e, options);
            return false;
        });
        return this;
    };

    //单击隐藏菜单
    $.fn.hideContextMenu = function() {
        try {
            menu.hide(); shadow.hide();
        } catch (e) { }
    };

    //菜单随着子窗口滚动2008-9-4
    $.fn.scrollMenu = function(ts_sctop) {
        if (document.all) {
            menu.css({ 'top': (curTop - ts_sctop + 130 + curTopx) + 'px', 'z-index': '9999999' });
            shadow.css({ 'top': (curTop - ts_sctop + 132 + curTopx) + 'px', 'z-index': '8888888' });
            window.status = menu.css('top');
        } else {
            menu.css({ 'top': curTop + 130 - ts_sctop, 'z-index': '9999999' });
            shadow.css({ 'top': curTop + 132 - ts_sctop, 'z-index': '8888888' });
        }
    };

    function display(index, trigger, e, options) {
        var cur = hash[index];
        content = $('#' + cur.id).find('ul:first').clone(true);
        content.css(cur.menuStyle).find('li').css(cur.itemStyle).hover(function() { $(this).css(cur.itemHoverStyle); },
                            function() { $(this).css(cur.itemStyle); })
                .find('img').css({ verticalAlign: 'middle', paddingRight: '2px' });

        // Send the content to the menu
        menu.html(content);

        // if there's an onShowMenu, run it now -- must run after content has been added
        // if you try to alter the content variable before the menu.html(), IE6 has issues
        // updating the content
        if (!!cur.onShowMenu) menu = cur.onShowMenu(e, menu);

        $.each(cur.bindings, function(id, func) {
            $('#' + id, menu).bind('click', function(e) {
                hide();
                func(trigger, currentTarget);
            });
        });

        var leftDoc = top.deskTabs.getFrameLeft().contentWindow.document;
        var ts_ctop = leftDoc.body.scrollTop;
        curTopx = ts_ctop;
        if (document.all) {
            ts_ctop = 0;
        }
        else {
            //获取最大的偏移值
            ts_ctop = Math.max($(leftDoc.body).scrollTop(), $(leftDoc.documentElement).scrollTop());
            curTop = e[cur.eventPosY];
            //检测是否超过父窗体显示区域
            var mTargetTop = e[cur.eventPosY] + 130 - ts_ctop;
            if (mTargetTop + menu.height() > $(top).height()) {
                ts_ctop += mTargetTop + menu.height() - $(top).height();
            }
        }
        menu.css({ 'left': e[cur.eventPosX], 'top': (e[cur.eventPosY] + 130 - ts_ctop), 'z-index': '9999999' }).show();
        if (cur.shadow) shadow.css({ width: menu.width(), height: menu.height(), left: e.pageX + 2, top: e.pageY + 132 - ts_ctop, 'z-index': '8888888' }).show();
        $(document).one('click', hide);
        if (top.deskTabs) {
            if (top.deskTabs.getFrameLeft()) {
                $(top.deskTabs.getFrameLeft().contentWindow.document).one('click', hide);
            }
            if (top.deskTabs.getFrameRight()) {
                $(top.deskTabs.getFrameRight().contentWindow.document).one('click', hide);
            }
        }
        //top.deskTabs.getFrameLeft().contentWindow.onscroll = function(){scrolled(top.deskTabs.getFrameLeft().contentWindow.document.body.scrollTop);}
    }

    function hide() { menu.hide(); shadow.hide(); }

    function scrolled(ts_sctop) {
        //if(document.all)alert(ts_sctop);
        if (document.all) {
            menu.css({ 'top': (curTop - ts_sctop + 130 + curTopx) + 'px', 'z-index': '9999999' });
            shadow.css({ 'top': (curTop - ts_sctop + 132 + curTopx) + 'px', 'z-index': '8888888' });
        } else {
            menu.css({ 'top': curTop + 130 - ts_sctop, 'z-index': '9999999' });
            shadow.css({ 'top': curTop + 132 - ts_sctop, 'z-index': '8888888' });
        }
    }

    // Apply defaults
    $.contextMenu = {
        defaults: function(userDefaults) {
            $.each(userDefaults, function(i, val) {
                if (typeof val == 'object' && defaults[i]) {
                    $.extend(defaults[i], val);
                }
                else defaults[i] = val;
            });
        }
    };

})(jQuery);

$(function(){$('div.contextMenu').hide();});


function init_contentMenu() {
    var objJQuery = arguments[0];
    var menuType = arguments[1];
    switch (menuType) {
        /* 优化业务右键配置与调用 参见admin/product/js/tree.js */ 
        /*case "3.3.1.0": //通用新建右键
        case "3.3.1.1": //业务分类右键
        case "3.3.1.2": //业务右键
        case "3.3.1.3": //业务频道/栏目右键
        case "3.3.1.4": //模块右键
        case "3.3.1.5": //通用修改、删除右键
        var key = 'menu' + menuType.replace(/\./g, "_");
        $('body').append(contextMenu[key].getHTML());
        objJQuery.find('li').each(function(obj) {
        $(this).contextMenu(key, {
        bindings: contextMenuBind[key],
        onShowMenu: (typeof contextMenuShow != 'undefined') ? contextMenuShow[key] : null
        });
        });
        break;*/ 

        case '3.5.1':
            $('body').append('<div style="display:none;"><div id="menu3_5_1" style="z-index:999999999"><ul><li id="add">添加子级菜单</li><li id="edit">修改菜单内容</li><hr size="1" style="height:1px; color:#CCCCCC; margin:0px; padding:0px;" /><li id="delete">删除菜单项目</li></ul></div></div>');
            objJQuery.find('li').each(function(obj) {
                $(this).contextMenu('menu3_5_1', {
                    bindings: {
                        'add': function(obj) {
                            top.fn_add_div('./System/CreateMenu.html?ParentId=' + obj.value, null, null, 780, 300, '添加子菜单');
                        },
                        'edit': function(obj) {
                            top.deskTabs.getFrameRight().contentWindow.location.href = './System/EditMenu.html?MenuId=' + obj.value;
                        },
                        'delete': function(obj) {
                            if (confirm('警告：您确定要删除选中菜单吗？\n\n该操作将无法恢复，请慎重选择！\n\n')) {
                                var delAjax = new top.AJAXFunction({ url: './service/1.3.5.1.8', param: { menuId: obj.value} });
                                delAjax.SuFun = function(json) {
                                    if (json.d.Status == 10007) {
                                        top.deskTabs.getFrameLeft().contentWindow.RefreshTreeNode();
                                    }
                                };
                                delAjax.SendFun();
                            }
                        }
                    }
                });
            });
            break;

        case '3.5.2':
            $('body').append('<div style="display:none;"><div id="role3_5_2" style="z-index:999999999"><ul><li id="addRole">增加角色</li><li id="editRole">修改角色</li><li id="delRole">删除角色</li><hr size="1" style="height:1px; color:#CCCCCC; margin:0px; padding:0px;" /><li id="setResPop">设置资源权限</li><li id="setProductPop">设置业务权限</li><li id="setMenuPop">设置菜单权限</li><hr size="1" style="height:1px; color:#CCCCCC; margin:0px; padding:0px;" /><li id="addThisRole">添加本角色用户</li><li id="addOtherRole">添加其它用户</li></ul></div></div>');
            objJQuery.find('div.title').each(function(obj) {
                $(this).contextMenu('role3_5_2', {
                    bindings: {
                        'addRole': function(obj) {
                            top.fn_add_div('./System/CreateRole.html', null, ($(document).height() - 300) / 2, 780, 150, '增加角色');
                        },
                        'editRole': function(obj) {
                            top.deskTabs.getFrameRight().contentWindow.location.href = './System/EditRole.html?RoleId=' + $(obj).parent().attr('value') + '&RoleName=' + $(obj).attr('roleName') + '&RoleDescription=' + $(obj).attr('roleDescription');
                        },
                        'delRole': function(obj) {
                            var roleName = $(obj).attr('roleName');
                            var roleDescription = $(obj).attr('roleDescription');
                            var tit = roleDescription + '[' + roleName + ']';
                            if (confirm('警告：您确定要删除系统角色【' + tit + '】吗？\n\n该操作将无法恢复，请慎重选择！\n\n')) {
                                var delAjax = new top.AJAXFunction({ url: './service/1.3.5.2.3', param: { roleName: roleName} });
                                delAjax.SuFun = function(json) {
                                    if (json.d.Status == 10031) {
                                        top.deskTabs.getFrameLeft().contentWindow.location.href = './System/RoleTree.html';
                                        top.deskTabs.getFrameRight().contentWindow.location.href = './System/EditRole.html?RoleId=0';
                                    }
                                };
                                top.showMask();
                                delAjax.SendFun();
                            }
                        },
                        'addThisRole': function(obj) {
                            var roleName = $(obj).attr('roleName');
                            var roleDescription = $(obj).attr('roleDescription');
                            var tit = roleDescription + '[' + roleName + ']';
                            top.fn_add_div('./System/AddUsersInRole.html?roleName=' + roleName + '&tit=' + tit, null, ($(document).height() - 450) / 2, 780, 450, '添加用户到角色：' + tit);
                        },
                        'addOtherRole': function(obj) {
                            top.fn_add_div('./System/AddUsersInRoles.html', null, ($(document).height() - 450) / 2, 780, 450, '添加用户到角色');
                        },
                        'setResPop': function(obj) {
                            top.deskTabs.getFrameRight().contentWindow.location.href = './System/EditResRole.html?RoleId=' + $(obj).parent().attr('value') + '&RoleName=' + $(obj).attr('roleName') + '&RoleDescription=' + $(obj).attr('roleDescription');
                        },
                        'setProductPop': function(obj) {
                            top.deskTabs.getFrameRight().contentWindow.location.href = './System/EditProductRole.html?RoleId=' + $(obj).parent().attr('value') + '&RoleName=' + $(obj).attr('roleName') + '&RoleDescription=' + $(obj).attr('roleDescription');
                        },
                        'setSoftPop': function(obj) {
                            top.deskTabs.getFrameRight().contentWindow.location.href = './System/EditSoftRole.html?RoleId=' + $(obj).parent().attr('value') + '&RoleName=' + $(obj).attr('roleName') + '&RoleDescription=' + $(obj).attr('roleDescription');
                        },
                        'setMenuPop': function(obj) {
                            top.deskTabs.getFrameRight().contentWindow.location.href = './System/EditMenuRole.html?RoleId=' + $(obj).parent().attr('value') + '&RoleName=' + $(obj).attr('roleName') + '&RoleDescription=' + $(obj).attr('roleDescription');
                        }
                    }
                });
            });
            break;

        //接入服务器菜单            
        case '3.99.1.4':
            $('body').append('<div style="display:none;"><div id="menu3_99_1_4" style="z-index:999999999"><ul>\
			<li id="edit">修改</li>\
			<li id="delete">删除</li>\
			<li id="add">添加</li>\
			<hr size="1" style="height:1px; color:#CCCCCC; margin:0px; padding:0px;" />\
			<li id="refreshCache">清除本业务缓存</li>\
			<li id="clear">缓存配置</li>\
			</ul></div></div>');
            objJQuery.find('li').each(function(obj) {
                $(this).contextMenu('menu3_99_1_4', {
                    bindings: {
                        'clear': function(obj) {
                            top.deskTabs.getFrameRight().contentWindow.location.href = './Assist/ServerCache.html?sid=' + obj.value;
                        },
                        'add': function(obj) {
                            top.deskTabs.getFrameRight().contentWindow.location.href = './Assist/EntSrvEdit.html';
                        },
                        'edit': function(obj) {
                            top.deskTabs.getFrameRight().contentWindow.location.href = './Assist/EntSrvEdit.html?t=edit&sid=' + obj.value;
                        },
                        'refreshCache': function(obj) {
                            top.deskTabs.getFrameRight().contentWindow.location.href = '../API/clearcache.ashx?sid=' + obj.value;
                        },
                        'delete': function(obj) {
                            if (confirm(('删除本业务配置信息“' + $.trim($(obj).children().text()) + '”可能导致该业务无法访问！\n确定要删除？'))) {
                                var ajaxClass = new AJAXFunction({
                                    url: './service/1.3.99.1.2',
                                    param: { serviceid: obj.value }
                                });
                                ajaxClass.SuFun = function(data, status) {
                                    alert('删除成功');
                                    top.deskTabs.getFrameLeft().contentWindow.location.reload();
                                };
                                ajaxClass.SendFun();
                            }
                        }
                    }
                });
            });
            break;
        default:
            break;
    }
}