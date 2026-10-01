import pg from 'pg';

const pgPool = new pg.Pool({
  host: 'localhost',
  port: 5432,
  database: 'tixflow',
  user: 'tixflow',
  password: 'tixflow_dev',
});

async function main() {
  console.log('=== TixFlow Post-Load-Test Verification ===\n');
  let exitCode = 0;

  // 1. Check for double-reservations: same buyer + same tier should have at most 1 order
  const dupes = await pgPool.query(`
    SELECT "BuyerId", "TicketTierId", COUNT(*) as cnt
    FROM "Orders"
    WHERE "Status" != 'Cancelled'
    GROUP BY "BuyerId", "TicketTierId"
    HAVING COUNT(*) > 1
  `);

  if (dupes.rows.length > 0) {
    console.log(`FAIL: ${dupes.rows.length} buyer(s) with duplicate reservations:`);
    dupes.rows.forEach(r =>
      console.log(`  Buyer ${r.BuyerId} / Tier ${r.TicketTierId}: ${r.cnt} orders`)
    );
    exitCode = 1;
  } else {
    console.log('PASS: Zero double-reservations detected');
  }

  // 2. Count orders by status
  const orderStats = await pgPool.query(`
    SELECT "Status", COUNT(*) as cnt
    FROM "Orders"
    GROUP BY "Status"
    ORDER BY cnt DESC
  `);
  console.log('\nOrder breakdown by status:');
  orderStats.rows.forEach(r => console.log(`  ${r.Status}: ${r.cnt}`));

  // 3. Total tickets created
  const ticketCount = await pgPool.query(`SELECT COUNT(*) as cnt FROM "Tickets"`);
  console.log(`\nTotal tickets created: ${ticketCount.rows[0].cnt}`);

  // 4. Verify ticket count matches confirmed orders
  const confirmedOrders = await pgPool.query(`
    SELECT COALESCE(SUM("Quantity"), 0) as total_qty
    FROM "Orders"
    WHERE "Status" = 'Confirmed'
  `);
  console.log(`Total confirmed order quantity: ${confirmedOrders.rows[0].total_qty}`);

  // 5. Check tier supply vs tickets created
  const tierCheck = await pgPool.query(`
    SELECT t."Id", t."Name", t."TotalSupply",
           COUNT(tk."Id") as tickets_created
    FROM "TicketTiers" t
    LEFT JOIN "Tickets" tk ON tk."TicketTierId" = t."Id"
    GROUP BY t."Id", t."Name", t."TotalSupply"
  `);

  console.log('\nTier inventory check:');
  tierCheck.rows.forEach(r => {
    const ok = parseInt(r.tickets_created) <= r.TotalSupply;
    console.log(`  ${r.Name}: ${r.tickets_created}/${r.TotalSupply} ${ok ? 'OK' : 'OVERSOLD'}`);
    if (!ok) exitCode = 1;
  });

  // 6. Check for any 500-class errors in orders (orders stuck in weird states)
  const stuckOrders = await pgPool.query(`
    SELECT COUNT(*) as cnt FROM "Orders"
    WHERE "Status" NOT IN ('Reserved', 'Confirmed', 'Cancelled', 'Expired')
  `);
  if (parseInt(stuckOrders.rows[0].cnt) > 0) {
    console.log(`\nWARN: ${stuckOrders.rows[0].cnt} orders in unexpected status`);
  }

  console.log(`\n=== Verification ${exitCode === 0 ? 'PASSED' : 'FAILED'} ===`);

  await pgPool.end();
  process.exit(exitCode);
}

main().catch(err => {
  console.error(err);
  process.exit(1);
});
