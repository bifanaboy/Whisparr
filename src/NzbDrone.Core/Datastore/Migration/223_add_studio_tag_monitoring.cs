using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(223)]
    public class add_studio_tag_monitoring : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Create.Table("StudioTagMonitoring")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("StudioId").AsInt32().NotNullable()
                .WithColumn("TagId").AsInt32().NotNullable()
                .WithColumn("CreatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
                .WithColumn("UpdatedAt").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime);

            Create.ForeignKey("FK_StudioTagMonitoring_Studio")
                .FromTable("StudioTagMonitoring").ForeignColumn("StudioId")
                .ToTable("Series").PrimaryColumn("Id")
                .OnDelete(System.Data.Rule.Cascade);

            Create.ForeignKey("FK_StudioTagMonitoring_Tag")
                .FromTable("StudioTagMonitoring").ForeignColumn("TagId")
                .ToTable("Tags").PrimaryColumn("Id")
                .OnDelete(System.Data.Rule.Cascade);

            Create.Index("IX_StudioTagMonitoring_StudioId_TagId")
                .OnTable("StudioTagMonitoring")
                .OnColumn("StudioId").Ascending()
                .OnColumn("TagId").Ascending()
                .WithOptions().Unique();
        }
    }
}