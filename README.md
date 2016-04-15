# ServiceBus Extensions

## Introduction

This assembly contains some basic Quality of Life enhancements for use with Microsoft ServiceBus

It supports
  1. Mass completion of every message in a subscription
  2. Processing of raw `BrokeredMessage`
  3. Auto-renewing of `BrokeredMessage` based on its half life
  4. From a POCO make a `BrokeredMessage` and optionaly add in the POCO properties to the `BrokeredMessage` properties

## Our use case

We do a fair amount of push notifications between different systems via ServiceBus. We needed these kind of **macros** to avoid writing them over and over for all of our micro services.

## Installation

You can obtain it [through Nuget](https://www.nuget.org/packages/Pandora.ServiceBusExtensions/) with:

    Install-Package Pandora.ServiceBusExtensions

Or **clone** this repo and reference it.
