using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace jll.emea.crm.DocGeneration
{
    class Watermark
    {
        public string Add(string data, string watermark, int fontSize, string color)
        {
            try
            {              
                Image image = Base64ToImage(data);

                using (Graphics gr = Graphics.FromImage(image))
                {
                    Font myFont = new Font("Arial", fontSize, FontStyle.Regular, GraphicsUnit.Pixel);                   
                    Brush brush = new SolidBrush((Color)new ColorConverter().ConvertFromString(color));
                    gr.DrawString(watermark, myFont, brush, 10, 10);

                    using (var ms = new MemoryStream())
                    {
                        image.Save(ms, ImageFormat.Jpeg);
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                return data;
            }
        }


        private Image Base64ToImage(string base64String)
        {
            byte[] imageBytes = Convert.FromBase64String(base64String);
            using (var ms = new MemoryStream(imageBytes, 0, imageBytes.Length))
            {                
                ms.Write(imageBytes, 0, imageBytes.Length);
                Image image = Image.FromStream(ms, true);
                return image;
            }
        }
    }
}
