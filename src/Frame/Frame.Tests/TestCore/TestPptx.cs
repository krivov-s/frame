// using FluentAssertions;
// using PptxTemplater;
// using System.Text.RegularExpressions;
//
// namespace Frame.Tests.TestCore
// {
//     public class TestPptx
//     {
//         const string TemplateFileName = "..\\..\\..\\files\\template.pptx";
//         const string ResultFileNameTag = "..\\..\\..\\files\\processed_tag.pptx";
//         const string ResultFileNamePic = "..\\..\\..\\files\\processed_pic.pptx";
//         const string PictureFileName = "..\\..\\..\\files\\nature.jpg";
//         const string NewValue = "Замена";
//
//         [Fact]
//         public void Replace_Tag_Ret_True()
//         {
//             File.Delete(ResultFileNameTag);
//             File.Copy(TemplateFileName, ResultFileNameTag);
//             using Pptx pptx = new(ResultFileNameTag, FileAccess.ReadWrite);
//             int nSlides = pptx.SlidesCount();
//             nSlides.Should().Be(2);
//             IEnumerable<PptxSlide> slides = pptx.GetSlides();
//             slides.Count().Should().Be(2);
//             PptxSlide slide = slides.First();
//             slide.Should().NotBeNull();
//             PptxSlide? slide1 = pptx.GetSlide(0);
//             slide1.Should().NotBeNull();
//             if (slide1 is not null)
//             {
//                 slide1.ReplaceTag("{{obj.Name}}", NewValue, PptxSlide.ReplacementType.Global);
//                 IEnumerable<string> texts = slide1.GetTexts();
//                 texts.First().Should().Be(NewValue);
//
//                 slide1.ReplaceTag("{{coolname}}", NewValue, PptxSlide.ReplacementType.Global);
//                 texts = slide1.GetTexts();
//                 texts.ElementAt(1).Should().Be($"Департамент {NewValue} людей");
//             }
//
//             PptxSlide? slide2 = pptx.GetSlide(1);
//             slide2.Should().NotBeNull();
//             if (slide2 != null)
//             {
//                 slide2.ReplaceTag("{{obj.Name[]}}", NewValue, PptxSlide.ReplacementType.Global);
//                 IEnumerable<string> texts = slide2.GetTexts();
//                 texts.First().Should().Be($"Таблица {NewValue}");
//
//                 slide2.ReplaceTag("{{obj.Val1}}", NewValue, PptxSlide.ReplacementType.Global);
//                 texts = slide2.GetTexts();
//                 texts.ElementAt(6).Should().Be(NewValue);
//                 texts.ElementAt(8).Should().Be(NewValue);
//
//                 slide2.ReplaceTag("{{obj.Val2}}", NewValue, PptxSlide.ReplacementType.Global);
//                 texts = slide2.GetTexts();
//                 texts.ElementAt(7).Should().Be($"{NewValue}");
//                 texts.ElementAt(9).Should().Be($"{NewValue}");
//             }
//             pptx.Save();
//             pptx.Close();
//         }
//
//         [Fact]
//         public void Replace_Pic_Ret_True()
//         {
//             File.Delete(ResultFileNamePic);
//             File.Copy(TemplateFileName, ResultFileNamePic);
//             using (Pptx pptx = new(ResultFileNamePic, FileAccess.ReadWrite))
//             {
//                 PptxSlide? slidepic = pptx.GetSlide(0);
//                 slidepic.Should().NotBeNull();
//                 if (slidepic is not null)
//                 {
//                     slidepic.ReplacePicture("{{picture1jpeg}}", PictureFileName, "image/jpeg");
//                 }
//                 pptx.Save();
//                 pptx.Close();
//             }
//         }
//
//         [Fact]
//         public void Iterate_Tags()
//         {
//             File.Delete(ResultFileNameTag);
//             File.Copy(TemplateFileName, ResultFileNameTag);
//             using Pptx pptx = new(ResultFileNameTag, FileAccess.ReadWrite);
//             int nSlides = pptx.SlidesCount();
//             nSlides.Should().Be(2);
//             IEnumerable<PptxSlide> slides = pptx.GetSlides();
//             PptxSlide? slide1 = pptx.GetSlide(0);
//             if (slide1 is not null)
//             {
//                 IEnumerable<string> texts = slide1.GetTexts();
//
//                 foreach (string text in texts)
//                 {
//                     MatchCollection matches = Regex.Matches(text, "\\{\\{(.*?)\\}\\}");
//         
//                     foreach (Match match in matches) 
//                     {
//                         Console.WriteLine(match.Groups[1].Value);
//                     }                
//                 }
//                 
//                 // slide1.ReplaceTag("{{obj.Name}}", NewValue, PptxSlide.ReplacementType.Global);
//                 // IEnumerable<string> texts = slide1.GetTexts();
//                 // texts.First().Should().Be(NewValue);
//                 //
//                 // slide1.ReplaceTag("{{coolname}}", NewValue, PptxSlide.ReplacementType.Global);
//                 // texts = slide1.GetTexts();
//                 // texts.ElementAt(1).Should().Be($"Департамент {NewValue} людей");
//             }
//         }
//         
//         ~TestPptx()
//         {
//             try
//             {
//                 File.Delete(ResultFileNameTag);
//                 File.Delete(ResultFileNamePic);
//             }
//             catch(Exception ex)
//             {
//                 Console.WriteLine(ex.ToString());
//             }
//             finally
//             {
//             }
//         }
//     }
// }
