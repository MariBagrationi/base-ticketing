import pg from 'pg';
import Redis from 'ioredis';
import jwt from 'jsonwebtoken';
import crypto from 'crypto';
import fs from 'fs';

const USER_COUNT = parseInt(process.env.USERS || '2000', 10);
const TOTAL_SUPPLY = parseInt(process.env.SUPPLY || '10000', 10);
const JWT_KEY = 'TixFlow-Dev-Signing-Key-Replace-In-Production-Min32Chars!';
const JWT_ISSUER = 'TixFlow';
const JWT_AUDIENCE = 'TixFlow';

const pgPool = new pg.Pool({
  host: 'localhost',
  port: 5432,
  database: 'tixflow',
  user: 'tixflow',
  password: 'tixflow_dev',
});

const redis = new Redis({ host: 'localhost', port: 6379 });

async function main() {
  console.log(`Seeding ${USER_COUNT} users, tier with ${TOTAL_SUPPLY} tickets...`);

  // Clean previous test data
  await pgPool.query(`DELETE FROM "MintJobs"`);
  await pgPool.query(`DELETE FROM "Tickets"`);
  await pgPool.query(`DELETE FROM "Orders"`);
  await pgPool.query(`DELETE FROM "TicketTiers"`);
  await pgPool.query(`DELETE FROM "Events"`);
  await pgPool.query(`DELETE FROM "Users"`);
  console.log('Cleaned previous data.');

  // Create organizer
  const organizerId = crypto.randomUUID();
  await pgPool.query(
    `INSERT INTO "Users" ("Id", "WalletAddress", "CreatedAt") VALUES ($1, $2, NOW())`,
    [organizerId, '0x' + 'aa'.repeat(20)]
  );

  // Create event
  const eventId = crypto.randomUUID();
  await pgPool.query(
    `INSERT INTO "Events" ("Id", "Name", "Description", "OrganizerId", "StartsAt", "VenueName")
     VALUES ($1, $2, $3, $4, NOW() + INTERVAL '30 days', $5)`,
    [eventId, 'Load Test Concert', 'Load test event for burst traffic simulation', organizerId, 'Colosseum Arena']
  );

  // Create tier
  const tierId = crypto.randomUUID();
  await pgPool.query(
    `INSERT INTO "TicketTiers" ("Id", "EventId", "Name", "PriceUsdc", "TotalSupply")
     VALUES ($1, $2, $3, $4, $5)`,
    [tierId, eventId, 'General Admission', 25.00, TOTAL_SUPPLY]
  );

  // Create users + JWTs + admission tokens
  const users = [];
  const batchSize = 200;

  for (let batch = 0; batch < USER_COUNT; batch += batchSize) {
    const end = Math.min(batch + batchSize, USER_COUNT);
    const promises = [];

    for (let i = batch; i < end; i++) {
      const userId = crypto.randomUUID();
      const wallet = '0x' + crypto.randomBytes(20).toString('hex');

      // Insert user
      promises.push(
        pgPool.query(
          `INSERT INTO "Users" ("Id", "WalletAddress", "CreatedAt") VALUES ($1, $2, NOW())`,
          [userId, wallet]
        )
      );

      // Generate auth JWT
      const authToken = jwt.sign(
        { sub: userId, wallet, jti: crypto.randomUUID() },
        JWT_KEY,
        { issuer: JWT_ISSUER, audience: JWT_AUDIENCE, expiresIn: '1h' }
      );

      // Generate admission token
      const tokenId = crypto.randomUUID();
      const admissionToken = jwt.sign(
        {
          purpose: 'admission',
          eventId,
          userId,
          jti: tokenId,
          iat: Math.floor(Date.now() / 1000),
        },
        JWT_KEY,
        { issuer: JWT_ISSUER, audience: JWT_AUDIENCE, expiresIn: '30m' }
      );

      // Store admission token in Redis
      const redisKey = `admission:${tokenId}`;
      promises.push(
        redis.pipeline()
          .hset(redisKey, 'userId', userId, 'eventId', eventId, 'consumed', 'false')
          .expire(redisKey, 1800)
          .exec()
      );

      users.push({
        userId,
        wallet,
        jwt: authToken,
        admissionToken,
        eventId,
        tierId,
      });
    }

    await Promise.all(promises);
    process.stdout.write(`\r  Created ${end}/${USER_COUNT} users`);
  }

  console.log('\nWriting users.json...');
  fs.writeFileSync(
    new URL('./users.json', import.meta.url),
    JSON.stringify(users, null, 0)
  );

  console.log(`Done. ${users.length} users seeded.`);
  console.log(`  Event ID: ${eventId}`);
  console.log(`  Tier ID:  ${tierId}`);
  console.log(`  Supply:   ${TOTAL_SUPPLY}`);

  await pgPool.end();
  await redis.quit();
}

main().catch(err => {
  console.error(err);
  process.exit(1);
});
