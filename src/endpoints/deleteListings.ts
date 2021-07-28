/**
 * @name Delete Listings
 * @url /api/:world/:item/delete
 */

import { ParameterizedContext } from "koa";
import { Collection } from "mongodb";
import { TrustedSourceManager } from "../db/TrustedSourceManager";
import { GenericUpload } from "../models/GenericUpload";
import { HttpStatusCodes } from "../models/HttpStatusCodes";

import sha from "sha.js";
import { Logger } from "winston";
import { BlacklistManager } from "../db/BlacklistManager";
import { DeleteRequest } from "../models/DeleteRequest";
import { MarketBoardItemListing } from "../models/MarketBoardItemListing";

export async function deleteListings(
	ctx: ParameterizedContext,
	logger: Logger,
	blacklistManager: BlacklistManager,
	trustedSourceManager: TrustedSourceManager,
	worldMap: Map<string, number>,
	recentData: Collection,
) {
	// validate API key
	const apiKey = ctx.get("Authorization"); // TODO: require the authorization header on uploads, too
	const client = await trustedSourceManager.get(apiKey);
	if (!apiKey || !client) {
		ctx.throw(HttpStatusCodes.FORBIDDEN);
	}

	// parse world ID
	let worldID = ctx.params.world;

	if (worldID && !parseInt(worldID)) {
		worldID = worldMap.get(worldID.charAt(0).toUpperCase() + worldID.substr(1));
		if (!worldID && typeof worldID === "string") {
			worldID = worldMap.get(
				worldID.charAt(0).toUpperCase() + worldID.substr(1).toLowerCase(),
			);
		}
	} else if (parseInt(worldID)) {
		worldID = parseInt(worldID);
	}

	if (typeof worldID !== "number") {
		logger.warn(`Got delete request with bad world ID "${worldID}".`);
		ctx.throw(HttpStatusCodes.BAD_REQUEST);
	}

	const itemID = parseInt(ctx.params.item as string);

	// parse the request body
	const uploadData: GenericUpload = ctx.request.body;

	if (!uploadData.uploaderID) {
		ctx.throw(HttpStatusCodes.BAD_REQUEST);
	}

	uploadData.uploaderID = sha("sha256")
		.update(uploadData.uploaderID.toString())
		.digest("hex");

	if (await blacklistManager.has(uploadData.uploaderID)) {
		ctx.body = "Success";
		return;
	}

	const deleteRequest = uploadData as DeleteRequest;

	// delete the listing
	logger.info(`Received listing delete request from user ${uploadData.uploaderID} using ${client.sourceName} to item ${itemID} on world ${worldID}.`);
	const itemData: { listings: MarketBoardItemListing[] } = await recentData.findOne({ worldID, itemID });
	if (itemData != null) {
		const listingIndex = itemData.listings.findIndex((listing) => {
			return listing.retainerID === deleteRequest.retainerID
				&& listing.quantity === deleteRequest.quantity
				&& listing.pricePerUnit === deleteRequest.pricePerUnit;
		});

		itemData.listings.splice(listingIndex, 1);

		await recentData.updateMany(
			{ worldID, itemID },
			{
				$set: {
					listings: itemData.listings,
				},
			},
		);

		logger.info(`Fulfilled listing delete request from user ${uploadData.uploaderID} using ${client.sourceName} to item ${itemID} on world ${worldID}.`);
	}

	ctx.body = "Success";
}