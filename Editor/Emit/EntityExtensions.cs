namespace Obfuz.Emit
{
    public static class EntityExtensions
    {
        public static T GetEntity<T>(this IGroupByModuleEntity entity) where T : IGroupByModuleEntity, new()
        {
            return entity.Manager.GetEntity<T>(entity.Module);
        }

        public static DefaultMetadataImporter GetDefaultModuleMetadataImporter(this IGroupByModuleEntity entity)
        {
            return entity.GetEntity<DefaultMetadataImporter>();
        }
    }
}
