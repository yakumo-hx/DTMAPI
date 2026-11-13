using System;

namespace DTMAPI.MonsterContent
{
    // 项目和发布内容共用字段读取，资源的取得方式由各自的入口提供。
    internal sealed class ContentReadContext
    {
        private readonly Func<string, string, string, ContentJson, byte[]> readImage;
        private readonly Func<object?, string, ContentJson, MonsterSound> readSound;
        private readonly Action<string, ContentJson> checkDropSource;
        private readonly Func<string, string, ContentJson, string> resolvePool;

        internal ContentReadContext(ContentJson json, string imageField,
            Func<string, string, string, ContentJson, byte[]> readImage,
            Func<object?, string, ContentJson, MonsterSound> readSound,
            Action<string, ContentJson> checkDropSource,
            Func<string, string, ContentJson, string> resolvePool)
        {
            Json = json;
            ImageField = imageField;
            this.readImage = readImage;
            this.readSound = readSound;
            this.checkDropSource = checkDropSource;
            this.resolvePool = resolvePool;
        }

        internal ContentJson Json { get; }
        internal string ImageField { get; }
        internal byte[] ReadImage(string key, string field, string message) => readImage(key, field, message, Json);
        internal MonsterSound ReadSound(object? value, string field) => readSound(value, field, Json);
        internal void CheckDropSource(string source) => checkDropSource(source, Json);
        internal string ResolvePool(string source, string id) => resolvePool(source, id, Json);
        internal ContentReadContext WithJson(ContentJson json) => new ContentReadContext(json, ImageField, readImage, readSound, checkDropSource, resolvePool);
    }
}
