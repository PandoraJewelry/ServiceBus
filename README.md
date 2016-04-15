# Azure ServiceBus Extensions

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

### Historical Note

This was our first foray into open sourcing projects. The project's internal name was **Pandora.ServiceBusExtensions** so we kept the name when we moved the code to [GitHub](https://github.com/PandoraJewelry/ServiceBus). After examining other similar projects we determined to rename the project to **Pandora.ServiceBus** to mimic the Microsoft DLL name. We are maintaining the package name of **Pandora.ServiceBusExtensions** in difference to those already using it.

