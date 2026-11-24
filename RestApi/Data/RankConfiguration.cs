using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestApi.Models;

namespace RestApi.Data;

internal sealed class RankConfiguration : IEntityTypeConfiguration<Rank>
{
    public void Configure(EntityTypeBuilder<Rank> rank)
    {
        rank.ToTable("ranks");

        rank.HasKey(r => r.Id);
        rank.Property(r => r.Id).HasColumnName("id").HasIdentityOptions(startValue: 100);
        rank.HasIndex(r => r.Code).IsUnique();

        rank.Property(r => r.Code)
            .HasColumnName("code")
            .HasMaxLength(32);

        rank.Property(r => r.EnName)
            .HasColumnName("en_name")
            .HasMaxLength(64);

        rank.Property(r => r.RuName)
            .HasColumnName("ru_name")
            .HasMaxLength(64);

        rank.Property(r => r.MinXp)
            .HasColumnName("min_xp");

        rank.HasIndex(r => r.MinXp).IsUnique();

        rank.HasData(
            new Rank(1, "private", "Private", "Рядовой", 0),
            new Rank(2, "corporal", "Corporal", "Ефрейтор", 100),
            new Rank(3, "junior_sergeant", "Junior Sergeant", "Младший сержант", 250),
            new Rank(4, "sergeant", "Sergeant", "Сержант", 450),
            new Rank(5, "senior_sergeant", "Senior Sergeant", "Старший сержант", 700),
            new Rank(6, "sergeant_major", "Sergeant Major", "Старшина", 1_000),
            new Rank(7, "warrant_officer", "Warrant Officer", "Прапорщик", 1_400),
            new Rank(8, "senior_warrant_officer", "Senior Warrant Officer", "Старший прапорщик", 1_900),
            new Rank(9, "junior_lieutenant", "Junior Lieutenant", "Младший лейтенант", 2_500),
            new Rank(10, "lieutenant", "Lieutenant", "Лейтенант", 3_200),
            new Rank(11, "senior_lieutenant", "Senior Lieutenant", "Старший лейтенант", 4_000),
            new Rank(12, "captain", "Captain", "Капитан", 5_000),
            new Rank(13, "major", "Major", "Майор", 6_200),
            new Rank(14, "lieutenant_colonel", "Lieutenant Colonel", "Подполковник", 7_600),
            new Rank(15, "colonel", "Colonel", "Полковник", 9_200),
            new Rank(16, "major_general", "Major General", "Генерал-майор", 11_000),
            new Rank(17, "lieutenant_general", "Lieutenant General", "Генерал-лейтенант", 13_500),
            new Rank(18, "ace", "Ace", "Ас", 16_500),
            new Rank(19, "god", "God", "Бог", 20_000)
        );
    }
}