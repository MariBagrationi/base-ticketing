using Microsoft.EntityFrameworkCore;
using TixFlow.Domain.Entities;
using TixFlow.Infrastructure.Data;

namespace TixFlow.Api.Demo;

// Idempotent: events are keyed by fixed IDs, so restarting the API never duplicates them.
public static class DemoEventSeeder
{
    private const string OrganizerWallet = "0x0000000000000000000000000000000000c0ffee";

    public static async Task SeedAsync(TixFlowDbContext db, ILogger logger)
    {
        var organizer = await db.Users.FirstOrDefaultAsync(u => u.WalletAddress == OrganizerWallet);
        if (organizer is null)
        {
            organizer = new User { Id = Guid.NewGuid(), WalletAddress = OrganizerWallet, CreatedAt = DateTimeOffset.UtcNow };
            db.Users.Add(organizer);
        }

        var ids = Events.Select(e => e.Id).ToList();
        var existing = await db.Events.Where(e => ids.Contains(e.Id)).Select(e => e.Id).ToListAsync();
        var today = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
        var added = 0;

        foreach (var seed in Events.Where(e => !existing.Contains(e.Id)))
        {
            db.Events.Add(new Event
            {
                Id = seed.Id,
                Name = seed.Name,
                VenueName = seed.Venue,
                Description = seed.Description.Trim(),
                StartsAt = today.AddDays(seed.DaysFromNow).AddHours(seed.HourUtc),
                OrganizerId = organizer.Id,
                TicketTiers = seed.Tiers.Select(t => new TicketTier
                {
                    Id = Guid.NewGuid(),
                    EventId = seed.Id,
                    Name = t.Name,
                    PriceUsdc = t.Price,
                    TotalSupply = t.Supply
                }).ToList()
            });
            added++;
        }

        await db.SaveChangesAsync();
        if (added > 0)
            logger.LogInformation("Seeded {Count} demo events", added);
    }

    private record SeedTier(string Name, decimal Price, int Supply);

    private record SeedEvent(Guid Id, string Name, string Venue, int DaysFromNow, int HourUtc, string Description, SeedTier[] Tiers);

    private static readonly SeedEvent[] Events =
    [
        new(Guid.Parse("a1e0c001-5eed-4000-8000-000000000001"),
            "Neon Arches: Synthwave Under the Stars",
            "Arches Amphitheatre, Old Town",
            9, 16,
            """
            Neon Arches returns for its fourth season. For one night the old stone amphitheatre becomes a wall of analog synths, laser fans and VHS-drenched visuals projected across the arches. Headliners Midnight Arcade play their first full live set in three years, with support from Chrome Haze and a closing set by DJ Lumen.

            Doors open at 7 PM with a retro arcade lounge, a vinyl and cassette market, and food trucks along the promenade. The main programme runs from 8:30 PM until 1 AM. The venue is open-air, so bring a layer: autumn nights here are clear and cool.

            General Admission covers the upper tiers with a full view of the stage. Pit Standing puts you right at the barrier. VIP Terrace includes a private bar, reserved seating overlooking the stage, and a limited screen-printed poster. Every ticket is an NFT on Base and can only be resold for up to 10% above face value, so scalpers have nothing to gain.
            """,
            [new("General Admission", 45m, 1200), new("Pit Standing", 85m, 300), new("VIP Terrace", 160m, 60)]),

        new(Guid.Parse("a1e0c001-5eed-4000-8000-000000000002"),
            "Base Camp 2026: Onchain Builders Summit",
            "Riverside Convention Hall, Hall B",
            16, 6,
            """
            Base Camp is a two-day summit for people who ship onchain products. Expect no token pitches and no panels of five people agreeing with each other: just engineers, designers and founders showing what they built, what broke, and what they would do differently.

            Day one covers account abstraction in production, gas-sponsored onboarding, and a deep dive on running indexers that survive chain reorgs. Day two is hands-on, with workshops on smart-wallet UX, Solidity security reviews, and load-testing backends that have to stay up during a sold-out drop. The closing keynote is a candid post-mortem of a launch that handled 40,000 checkouts in ten minutes.

            Builder Pass includes both days, all workshops, lunch and the evening social. Founder Pass adds reserved front-row seating, a private office-hours slot with speakers, and the investor dinner. Student Pass covers both days and requires a valid student ID at the door. Wi-Fi, power at every seat, and good coffee are guaranteed.
            """,
            [new("Student Pass", 29m, 200), new("Builder Pass", 149m, 800), new("Founder Pass", 399m, 120)]),

        new(Guid.Parse("a1e0c001-5eed-4000-8000-000000000003"),
            "Symphony of Distortion: Rock Classics Live with Orchestra",
            "Grand Philharmonic Hall",
            23, 15,
            """
            A 60-piece symphony orchestra, a four-piece rock band and a 40-voice choir take on two decades of stadium anthems. Every arrangement was written for this production: think thunderous brass under guitar solos, string sections carrying the riffs, and a choir that turns every chorus into a cathedral.

            The setlist runs from late-70s arena rock through 90s alternative, with a few surprises the conductor refuses to reveal in advance. The performance lasts about two and a half hours, including a 20-minute interval. The hall's acoustics were restored last year, so every seat in the house sounds remarkable.

            Balcony seats offer the best view of the full orchestra. Stalls put you close enough to feel the timpani. Box Seats are private four-person boxes with table service and a glass of sparkling wine at the interval. Smart-casual dress is suggested but not required. Latecomers are seated at the interval only.
            """,
            [new("Balcony", 40m, 400), new("Stalls", 75m, 600), new("Box Seats", 220m, 40)]),

        new(Guid.Parse("a1e0c001-5eed-4000-8000-000000000004"),
            "Velocity Cup 2026: Esports Grand Final",
            "Velocity Arena",
            30, 12,
            """
            Eight months, 64 teams and one trophy. The Velocity Cup grand final brings the two best tactical shooter teams of the season to a sold-out arena for a best-of-five series played on stage, under the lights, with every round on a 30-metre LED wall.

            Defending champions Northwind face first-time finalists Sable Tide, a roster of rookies who knocked out three former champions on the way. Before the main event there is a showmatch featuring creators and pro veterans, plus a cosplay competition judged on stage. Shout-casting is live in the arena, and team merch drops at the concourse stands from noon.

            Upper Bowl seats see the whole stage and screen. Lower Bowl puts you close to the players' booths. Front Row + Meet & Greet includes floor seating, a signed team jersey, and a post-match photo session with the winning roster. The arena is all-ages; under-14s must be accompanied by an adult.
            """,
            [new("Upper Bowl", 20m, 3000), new("Lower Bowl", 55m, 1500), new("Front Row + Meet & Greet", 180m, 80)]),

        new(Guid.Parse("a1e0c001-5eed-4000-8000-000000000005"),
            "Late Night Laughs: Stand-Up Showcase",
            "The Basement Comedy Club",
            12, 18,
            """
            Five comedians, one microphone and a room that seats only 190 people. Late Night Laughs is the city's longest-running stand-up showcase, where touring headliners road-test new material next to the sharpest local acts before they make it big.

            This month's line-up is headlined by Mara Quinn, fresh off a sold-out festival run with her show about growing up above her parents' bakery. She is joined by four comics the club's booker describes as dangerously good. Your host for the night is resident MC Theo Banks, who has never once stuck to the running order.

            Standard seats are first come, first served from doors at 9:30 PM. Front Tables are reserved for groups and sit right by the stage; anyone in them should expect to be part of the show. Strictly 18+. Phones stay in pockets: filming is not allowed, so the jokes can stay rough around the edges.
            """,
            [new("Standard", 18m, 150), new("Front Tables", 30m, 40)]),

        new(Guid.Parse("a1e0c001-5eed-4000-8000-000000000006"),
            "Harvest Table: Wine & Food Festival",
            "Old Vine Estate, Kakheti Valley",
            44, 7,
            """
            Celebrate the end of the grape harvest with a full day among the vines. More than 30 family winemakers pour their newest vintages alongside amber wines aged in clay qvevri, the traditional vessels buried underground that give these wines their deep colour and texture.

            The festival grounds cover the estate courtyard and the terraced vineyard. Chefs from across the region cook over open fire all day: grilled meats, freshly baked bread from a clay oven, mountain cheeses and churchkhela made in front of you. Live folk polyphony fills the afternoon, and the grape-stomping contest at 4 PM is open to anyone brave enough.

            The Day Pass includes entry, a tasting glass and five tasting tokens. The Tasting Pass adds unlimited pours at every stand and a guided masterclass with a winemaker. The Winemaker's Dinner is a 48-seat candlelit supper in the estate cellar, with six courses paired with rare library wines. Shuttle buses run from the city centre from 9 AM.
            """,
            [new("Day Pass", 35m, 800), new("Tasting Pass", 70m, 300), new("Winemaker's Dinner", 190m, 48)]),

        new(Guid.Parse("a1e0c001-5eed-4000-8000-000000000007"),
            "Kinetic: Contemporary Dance Premiere",
            "Studio Theatre No. 9",
            58, 16,
            """
            Kinetic is a new full-length work by choreographer Lena Arkhipova for nine dancers, a cellist and a room full of motion sensors. Every movement on stage feeds a live generative score and light design, so no two performances sound or look the same.

            The piece follows a city waking up over one long morning, moving from stillness to rush-hour chaos and back again. It was developed over eighteen months of residencies and premieres here before a European tour. The performance runs 75 minutes with no interval and is followed by a short talk with the company in the foyer.

            The studio is an intimate black-box space with raked seating on three sides, so there are no bad seats. Premium tickets are in the first two rows and include the printed programme and a drink at the post-show talk. The performance contains strobe lighting and loud passages of sound.
            """,
            [new("Standard", 28m, 220), new("Premium", 48m, 80)])
    ];
}
