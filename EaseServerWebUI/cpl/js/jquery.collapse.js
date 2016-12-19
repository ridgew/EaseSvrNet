//$(document).ready(function(){  
//$("div.collapse").collapse();  
//});
//This simple jQuery plugin is for collapsing and expanding sections of content.
//"div.collapse"��˼�������ǽ������ǵĲ��Ӧ�õ���һ��class����Ϊcollapse��div��ǩ�ϡ���ֻ��ʹ��collapse��������򵥵ı�ʾ�۵���
//չ�������ݡ��������ĳ��ԭ��������Ҫ�ı���Щdiv�е�����ʾͷ�������ݵ�Ԫ�����ƣ�����������ı�һ���Ǹ�ͼ�꣬���ǿ�����������������
//$("div.collapse").collapse({header: "h2", content: "p", exapandIcon: "images/plus.gif", collapseIcon: "images/minus.gif"});
(function() {

    jQuery.fn.collapse = function(settings) {

        var cContainers = this;     //The jquery objects that contain our collapsable items.
        
        // define defaults and override with options, if available
        // by extending the default settings, we don't modify the argument
        settings = jQuery.extend({
         header: "h3",
         content: "ul",
         expandIcon: "images/plus.gif",
         collapseIcon: "images/minus.gif"
        }, settings);

        //Loop through the jquery objects (these are the elements that contain our items to collapse).
        return cContainers.each(function(){

            //This current dom element.
            var jDomElem = this;
            var headerDomElem = jQuery(settings.header, jDomElem);
            var contentDomElem = jQuery(settings.content, jDomElem);
            
            //Put the plus/minus icon in to the header.
            var expandIconDomElem = headerDomElem.prepend('<img src="' + settings.expandIcon + '" alt="" />');

            //When the header element is clicked.
            headerDomElem.click(function() {
            
                //Determine the correct expand/collapse icon src.
                var iconImgSrc = settings.expandIcon;
                if(contentDomElem.css("display")=="none") {
                    iconImgSrc = settings.collapseIcon;
                }
                
                //Take the header (the clicked item) and change the icon in it. We know this is the first element inside it because we put it there.
                jQuery(this.firstChild).attr("src", iconImgSrc); 
                
                //Show/hide the content.
                contentDomElem.toggle();
                
            });

            //Hide the content area.
            contentDomElem.hide();
            
        });
      
    };
  
})(jQuery);