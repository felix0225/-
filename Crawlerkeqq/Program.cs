using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Linq;
using AngleSharp.Html.Parser;

namespace Crawlerkeqq
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            Console.WriteLine("請輸入要取得免費課程的名稱，輸入完後按下Enter");
            var keyword = Console.ReadLine();

            Console.WriteLine($"取得免費的{keyword}課程");

            var client = new HttpClient();

            var baseUrl = $"https://ke.qq.com/course/list/{keyword}?price_min=0&price_max=0";
            var responseBody = client.GetStringAsync(baseUrl).GetAwaiter().GetResult();
            var parser = new HtmlParser();
            var doc = parser.ParseDocument(responseBody);

            //取得最大頁數
            var maxPage = Convert.ToInt32(doc.QuerySelector("body > section.main.autoM.clearfix > div > div.sort-page > a:nth-child(6)").InnerHtml);
            Console.WriteLine($"共有 {maxPage} 頁");
            
            var CourseList = new List<Course>();
            int i;
            for (i = 1; i <= maxPage; i++)
            {
                Console.WriteLine($"目前頁數 {i}");

                var Url = $"{baseUrl}&page={i}";
                responseBody = client.GetStringAsync(Url).GetAwaiter().GetResult();
                parser = new HtmlParser();
                doc = parser.ParseDocument(responseBody);

                var courseliList = doc.QuerySelectorAll("body > section.main.autoM.clearfix > div > div.market-bd.market-bd-6.course-list.course-card-list-multi-wrap.js-course-list > ul > li");
                foreach (var element in courseliList)
                {
                    var course = new Course();
                    var img = element.QuerySelector("a > img").GetAttribute("src");
                    course.ImgUrl = img.Contains("https") ? img : "https:" + img;

                    var h4 = element.QuerySelector("h4 > a");
                    var title = h4.InnerHtml;
                    Console.WriteLine($"課程名稱：{title}");
                    course.ClassName = title;

                    var href = h4.GetAttribute("href");
                    course.ClassLink = href;

                    var people = element.QuerySelector("div.item-line.item-line--bottom > span.line-cell.item-user.custom-string").InnerHtml.Trim().Replace("人最近报名", "");
                    if (people.Contains("万"))
                        people = people.Replace("万", "0000");
                    course.People = Convert.ToInt32(people);

                    CourseList.Add(course);
                }
            }

            //依報名人數排序
            var CourseSort = from c in CourseList
                             orderby c.People descending
                             select c;

            var fileTitle = $"騰訊課堂免費的{keyword}課程";

            //組 Html 檔案內容
            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html>");
            sb.Append("<html xmlns=\"http://www.w3.org/1999/xhtml\">");
            sb.Append("<head>");
            sb.Append("<meta charset=\"utf-8\" />");
            sb.Append($"<title>{fileTitle}</title>");
            sb.Append("</head>");
            sb.Append("<body>");
            sb.Append("<table style=\"width: 50%; margin: auto; border:3px #cccccc solid;\" border=\"1\">");
            sb.Append("<tr><th>No</th><th>圖片</th><th>課程名稱</th><th>報名人數</th><th>狀態</th></tr>");
            var no = 0;
            foreach (var item in CourseSort)
            {
                no++;
                sb.Append($"<tr><td>{no}</td><td><a href='{item.ClassLink}' target='_blank'><img src='{item.ImgUrl}'></a></td><td><a href='{item.ClassLink}' target='_blank'>{item.ClassName}</a></td><td>{item.People}</td><td></td></tr>");
            }
            sb.Append("</table>");
            sb.Append("</body>");
            sb.Append("</html>");

            //判斷檔案是否存在,如果存在先刪除
            var fileName = $"{fileTitle}.html";
            var fi = new FileInfo(fileName);
            if (fi.Exists) fi.Delete();

            Console.WriteLine($"輸出的檔案位置：{fileName}");

            //將結果輸出成 Html 檔
            File.WriteAllText(fileName, sb.ToString());
        }
    }
}
